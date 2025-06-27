using AccessLensApi.Common;
using AccessLensApi.Common.Jobs;
using AccessLensApi.Common.Services;
using AccessLensApi.Data;
using AccessLensApi.Features.Core.Interfaces;
using AccessLensApi.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AccessLensApi.Features.Scans
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScansController : BaseController
    {
        private readonly IJobQueue<ScanJob> _scanJobQueue;
        private readonly ILogger<ScansController> _logger;
        private readonly IRateLimiter _rateLimiter;
        private readonly RateLimitingOptions _rateOptions;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ScansController(
            ApplicationDbContext db, 
            IJobQueue<ScanJob> scanJobQueue,
            ILogger<ScansController> logger,
            IRateLimiter rateLimiter,
            IOptions<RateLimitingOptions> rateOptions,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IConfiguration config) : base(db)
        {
            _scanJobQueue = scanJobQueue;
            _logger = logger;
            _rateLimiter = rateLimiter;
            _rateOptions = rateOptions.Value;
            _env = env;
            _httpClient = httpClientFactory.CreateClient();
            _config = config;
        }

        [HttpPost("start")]
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> StartScan([FromBody] StartScanRequest request)
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            try
            {
                // Validate URL
                if (!Uri.TryCreate(request.SiteUrl, UriKind.Absolute, out var uri))
                {
                    return BadRequest(ApiResponse.ErrorResult("Invalid URL provided"));
                }

                // Create scan job
                var scanJob = new ScanJob
                {
                    UserEmail = userEmail,
                    SiteUrl = request.SiteUrl,
                    SiteName = request.SiteName ?? uri.Host,
                    SiteId = request.SiteId,
                    ScanTier = request.ScanTier ?? "free",
                    NotificationType = request.NotificationType ?? DetermineScanNotificationType(request.ScanTier),
                    Options = new ScanOptions
                    {
                        MaxPages = Math.Min(request.MaxPages ?? 10, 100), // Cap at 100 pages
                        MaxConcurrency = Math.Min(request.MaxConcurrency ?? 3, 5), // Cap at 5 concurrent
                        GenerateTeaser = request.GenerateTeaser ?? true,
                        GeneratePdf = request.GeneratePdf ?? true,
                        UrlPatterns = request.UrlPatterns ?? Array.Empty<string>(),
                        ExcludePatterns = request.ExcludePatterns ?? Array.Empty<string>()
                    }
                };

                // Enqueue the job
                await _scanJobQueue.EnqueueAsync(scanJob);

                _logger.LogInformation("Scan job {JobId} enqueued for user {UserEmail} and site {SiteUrl}", 
                    scanJob.Id, userEmail, request.SiteUrl);

                return Ok(ApiResponse<ScanJobResponse>.SuccessResult(new ScanJobResponse
                {
                    JobId = scanJob.Id,
                    Status = "Queued",
                    Message = "Scan has been queued and will be processed shortly"
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start scan for user {UserEmail}", userEmail);
                return StatusCode(500, ApiResponse.ErrorResult("Failed to start scan"));
            }
        }

        [HttpGet("status/{jobId}")]
        [Authorize(Policy = "Authenticated")]
        public IActionResult GetScanStatus(string jobId)
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            try
            {
                // In a real implementation, you'd check if the user owns this job
                var jobQueue = _scanJobQueue as InMemoryJobQueue<ScanJob>;
                var job = jobQueue?.GetJobStatus(jobId);

                if (job == null)
                {
                    return NotFound(ApiResponse.ErrorResult("Scan job not found"));
                }

                var response = new ScanJobStatusResponse
                {
                    JobId = job.Id,
                    Status = job.Status.ToString(),
                    CreatedAt = job.CreatedAt,
                    StartedAt = job.StartedAt,
                    CompletedAt = job.CompletedAt,
                    ErrorMessage = job.ErrorMessage,
                    Progress = CalculateProgress(job)
                };

                return Ok(ApiResponse<ScanJobStatusResponse>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get scan status for job {JobId}", jobId);
                return StatusCode(500, ApiResponse.ErrorResult("Failed to get scan status"));
            }
        }

        [HttpGet("jobs")]
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> GetUserJobs()
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            try
            {
                // Get all jobs for this user (in a real implementation, filter by user)
                var pendingJobs = await _scanJobQueue.GetPendingJobsAsync();
                var userJobs = pendingJobs.Where(j => j.UserEmail == userEmail);

                var jobResponses = userJobs.Select(job => new ScanJobStatusResponse
                {
                    JobId = job.Id,
                    Status = job.Status.ToString(),
                    CreatedAt = job.CreatedAt,
                    StartedAt = job.StartedAt,
                    CompletedAt = job.CompletedAt,
                    ErrorMessage = job.ErrorMessage,
                    SiteUrl = job.SiteUrl,
                    SiteName = job.SiteName,
                    Progress = CalculateProgress(job)
                });

                return Ok(ApiResponse<IEnumerable<ScanJobStatusResponse>>.SuccessResult(jobResponses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user jobs for {UserEmail}", userEmail);
                return StatusCode(500, ApiResponse.ErrorResult("Failed to get scan jobs"));
            }
        }

        private static double CalculateProgress(ScanJob job)
        {
            return job.Status switch
            {
                JobStatus.Pending => 0.0,
                JobStatus.Processing => 50.0, // Could be more sophisticated
                JobStatus.Completed => 100.0,
                JobStatus.Failed => 0.0,
                JobStatus.Cancelled => 0.0,
                _ => 0.0
            };
        }

        private static ScanNotificationType DetermineScanNotificationType(string? scanTier)
        {
            return scanTier?.ToLowerInvariant() switch
            {
                "free" => ScanNotificationType.RichWithPdf,     // Free scans get rich emails with PDF
                "premium" => ScanNotificationType.Basic,        // Premium users get basic notifications (they access via dashboard)
                "enterprise" => ScanNotificationType.Basic,     // Enterprise users get basic notifications
                null => ScanNotificationType.RichWithPdf,       // Default to rich for free tier
                _ => ScanNotificationType.Basic
            };
        }

        [HttpPost("starter")]
        [RequestSizeLimit(1_048_576)]
        public async Task<IActionResult> StarterScan([FromForm] StarterScanRequest request)
        {
            try
            {
                // 1. Captcha verification (skip in development)
                if (!_env.IsDevelopment())
                {
                    if (!await VerifyTurnstileAsync(request.CaptchaToken, HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty))
                        return BadRequest(new { error = "captcha_failed" });
                }

                // 2. Validate request
                var validationError = ValidateStarterRequest(request);
                if (validationError != null)
                    return validationError;

                var email = request.Email.Trim().ToLowerInvariant();
                var url = request.Url.Trim();
                var ip = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

                // 3. URL validation
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsUrlAllowed(uri))
                    return BadRequest(new { error = "URL not allowed." });

                // 4. Rate limiting
                var user = await GetOrCreateUserAsync(email);
                if (!await _rateLimiter.TryAcquireStarterAsync(ip, email, user.EmailVerified))
                    return StatusCode(429, new { error = "Rate limit exceeded" });

                try
                {
                    // 5. Email verification check
                    var verifyError = CheckVerification(user);
                    if (verifyError != null)
                        return verifyError;

                    // 6. Quota check (skip for first scan)
                    if (!user.FirstScan)
                    {
                        var paymentError = await CheckQuotaAsync(email);
                        if (paymentError != null)
                            return paymentError;
                    }

                    // 7. Update first scan flag
                    await UpdateFirstScanAsync(user);

                    // 8. Create scan job
                    var scanJob = new ScanJob
                    {
                        UserEmail = email,
                        SiteUrl = url,
                        SiteName = uri.Host,
                        ScanTier = "free",
                        NotificationType = ScanNotificationType.RichWithPdf,
                        Options = new ScanOptions
                        {
                            MaxPages = 5, // Starter scans are limited to 5 pages
                            MaxConcurrency = 2,
                            GenerateTeaser = true,
                            GeneratePdf = true,
                            UrlPatterns = Array.Empty<string>(),
                            ExcludePatterns = Array.Empty<string>()
                        }
                    };

                    // 9. Enqueue the job
                    await _scanJobQueue.EnqueueAsync(scanJob);

                    _logger.LogInformation("Starter scan job {JobId} enqueued for user {UserEmail} and site {SiteUrl}", 
                        scanJob.Id, email, url);

                    return Ok(new 
                    {
                        jobId = scanJob.Id,
                        status = "queued",
                        message = "Accessibility scan queued successfully"
                    });
                }
                finally
                {
                    _rateLimiter.ReleaseStarter();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start starter scan for email {Email}", request.Email);
                return StatusCode(500, new { error = "Failed to start scan" });
            }
        }

        // Helper methods copied from ScanController for marketing page protection
        private async Task<bool> VerifyTurnstileAsync(string token, string ip)
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new ("secret", _config["Turnstile:SecretKey"] ?? ""),
                new ("response", token)
            };

            if (!string.IsNullOrEmpty(ip))
            {
                fields.Add(new("remoteip", ip));
            }

            using var body = new FormUrlEncodedContent(fields);
            var res = await _httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify", body);

            var doc = await res.Content.ReadFromJsonAsync<JsonDocument>();
            bool ok = doc!.RootElement.GetProperty("success").GetBoolean();
            return ok;
        }

        private IActionResult? ValidateStarterRequest(StarterScanRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { error = "Email is required" });

            if (string.IsNullOrWhiteSpace(req.Url))
                return BadRequest(new { error = "URL is required" });

            if (!new EmailAddressAttribute().IsValid(req.Email))
                return BadRequest(new { error = "Invalid email format" });

            return null;
        }

        private bool IsUrlAllowed(Uri uri)
        {
            if (!_env.IsDevelopment())
            {
                if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
            {
                var ip = System.Net.IPAddress.Parse(uri.Host);
                if (System.Net.IPAddress.IsLoopback(ip)) return false;

                var bytes = ip.GetAddressBytes();
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    if (bytes[0] == 10 ||
                        bytes[0] == 192 && bytes[1] == 168 ||
                        bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                        bytes[0] == 169 && bytes[1] == 254)
                        return false;
                }
                if (ip.IsIPv6LinkLocal) return false;
            }

            return true;
        }

        private async Task<Features.Auth.Models.User> GetOrCreateUserAsync(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new Features.Auth.Models.User
                {
                    Email = email,
                    EmailVerified = false,
                    FirstScan = true,
                    CreatedAt = DateTime.UtcNow,
                    ScansUsed = 0,
                    ScanLimit = 1 // Free tier gets 1 scan
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            return user;
        }

        private IActionResult? CheckVerification(Features.Auth.Models.User user)
        {
            if (!user.EmailVerified && !user.FirstScan && user.Email != "derekbolyard@gmail.com")
            {
                return BadRequest(new { error = "needVerify", message = "Email verification required" });
            }
            return null;
        }

        private async Task<IActionResult?> CheckQuotaAsync(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && user.ScansUsed >= user.ScanLimit && user.Email != "derekbolyard@gmail.com")
            {
                return BadRequest(new { error = "needPayment", message = "Scan limit exceeded" });
            }
            return null;
        }

        private async Task UpdateFirstScanAsync(Features.Auth.Models.User user)
        {
            if (user.FirstScan)
            {
                user.FirstScan = false;
                user.ScansUsed = 1;
                await _db.SaveChangesAsync();
            }
            else
            {
                user.ScansUsed++;
                await _db.SaveChangesAsync();
            }
        }
    }

    public class StartScanRequest
    {
        public string SiteUrl { get; set; } = string.Empty;
        public string? SiteName { get; set; }
        public Guid? SiteId { get; set; }
        public int? MaxPages { get; set; }
        public int? MaxConcurrency { get; set; }
        public bool? GenerateTeaser { get; set; }
        public bool? GeneratePdf { get; set; }
        public string[]? UrlPatterns { get; set; }
        public string[]? ExcludePatterns { get; set; }
        public string? ScanTier { get; set; } // "free", "premium", "enterprise"
        public ScanNotificationType? NotificationType { get; set; }
    }

    public class ScanJobResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ScanJobStatusResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SiteUrl { get; set; }
        public string? SiteName { get; set; }
        public double Progress { get; set; }
    }

    public class StarterScanRequest
    {
        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [FromForm(Name = "cf-turnstile-response")]
        public string CaptchaToken { get; set; } = string.Empty;
    }
}
