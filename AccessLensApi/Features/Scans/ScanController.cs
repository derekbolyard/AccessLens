using AccessLensApi.Data;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Middleware;
using AccessLensApi.Models;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Net.WebRequestMethods;

namespace AccessLensApi.Features.Scans
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScanController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ICreditManager _creditManager;
        private readonly IA11yScanner _scanner;
        private readonly IPdfService _pdf;
        private readonly ILogger<ScanController> _logger;
        private readonly IRateLimiter _rateLimiter;
        private readonly RateLimitingOptions _rateOptions;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public ScanController(
            ApplicationDbContext dbContext,
            ICreditManager creditManager,
            IA11yScanner scanner,
            IPdfService pdf,
            ILogger<ScanController> logger,
            IRateLimiter rateLimiter,
            IOptions<RateLimitingOptions> rateOptions,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env,
            IConfiguration config,
            IEmailService emailService)
        {
            _dbContext = dbContext;
            _creditManager = creditManager;
            _scanner = scanner;
            _pdf = pdf;
            _logger = logger;
            _rateLimiter = rateLimiter;
            _rateOptions = rateOptions.Value;
            _httpClient = httpClientFactory.CreateClient();
            _env = env;
            _config = config;
            _emailService = emailService;
        }

        /// <summary>
        /// POST /api/scan/starter
        /// Body: { url, email }
        /// 
        /// 1) Validate URL and email.
        /// 2) Ensure User record exists.
        /// 3) If not verified & not firstScan: return needVerify.
        /// 4) If no quota: return needPayment.
        /// 5) Flip firstScan if this is the user’s first scan.
        /// 6) Run the a11y scanner (up to 5 pages).
        /// 7) Compute score from the first page’s results.
        /// 8) Generate and upload a PDF from the first page’s JSON.
        /// 9) Return { score, pdfUrl, teaserUrl }.
        /// </summary>
        [HttpPost("starter")]
        [RequestSizeLimit(1_048_576)]
        public async Task<IActionResult> Starter([FromForm] ScanRequest req)
        {
            try
            {
                if (!_env.IsDevelopment())
                {
                    if (!await VerifyTurnstileAsync(req.CaptchaToken, HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty))
                        return BadRequest("captcha_failed");
                }
                
                var validationError = ValidateRequest(req);
                if (validationError != null)
                    return validationError;

                var email = req.Email.Trim().ToLowerInvariant();
                var url = req.Url.Trim();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsUrlAllowed(uri))
                    return BadRequest(new { error = "URL not allowed." });

                var user = await GetOrCreateUserAsync(email);

                if (!await _rateLimiter.TryAcquireStarterAsync(ip, email, user.EmailVerified))
                    return StatusCode(429, new { error = "Rate limit exceeded" });

                // 3) If not verified & not firstScan: return needVerify
                var verifyError = CheckVerification(user);
                if (verifyError != null)
                    return verifyError;

                if (!user.FirstScan && user.Email != "derekbolyard@gmail.com")
                {
                    var paymentError = await CheckQuotaAsync(email);
                    if (paymentError != null)
                        return paymentError;
                }

                // 5) Flip firstScan if needed
                await UpdateFirstScanAsync(user);


                JsonObject scanResult;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_rateOptions.MaxScanDurationSeconds));
                try
                {
                    scanResult = await _scanner.ScanFivePagesAsync(url, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return StatusCode(500, new { error = "Scan timed out." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "A11y scan failed for URL: {Url}", url);
                    return StatusCode(500, new { error = "Accessibility scan failed." });
                }
                finally
                {
                    _rateLimiter.ReleaseStarter();
                }

                // 7) Compute score from the first page’s results
                int score;
                try
                {
                    score = ComputeScore(scanResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to compute score for URL: {Url}", url);
                    return StatusCode(500, new { error = "Failed to compute accessibility score." });
                }

                // 8) Generate and upload PDF from the first page’s JSON
                string pdfUrl;
                try
                {
                    pdfUrl = await GeneratePdfUrlAsync(url, scanResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PDF generation failed for URL: {Url}", url);
                    return StatusCode(500, new { error = "PDF generation failed." });
                }

                var teaser = ExtractTeaser(scanResult);

                await SaveReportAsync(scanResult, email, url, pdfUrl);
                await _emailService.SendScanResultEmailAsync(email, pdfUrl, score, teaser?.Url);

                return Ok(new { score, pdfUrl, teaser });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during scan request.");
                return StatusCode(500, new { error = "Internal server error." });
            }
        }

        /// <summary>
        /// POST /api/scan/full
        /// Body: { url, email, options }
        /// 
        /// Performs a comprehensive accessibility scan of all discoverable pages on a website.
        /// </summary>
        [HttpPost("full")]
        [Authorize(Policy = "Authenticated")]
        [RequestSizeLimit(2_097_152)] // 2MB limit for larger responses
        public async Task<IActionResult> FullSiteScan([FromBody] FullScanRequest req)
        {
            var validationError = ValidateFullScanRequest(req);
            if (validationError != null)
                return validationError;

            var email = req.Email.Trim().ToLowerInvariant();
            var url = req.Url.Trim();

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsUrlAllowed(uri))
                return BadRequest(new { error = "URL not allowed." });

            var user = await GetOrCreateUserAsync(email);

            // Check if user has premium access for full scans

            if (req.Email != "derekbolyard@gmail.com")
            {
                var hasFullAccess = await _creditManager.HasPremiumAccessAsync(email);
                if (!hasFullAccess)
                    return BadRequest(new { error = "Full site scanning requires premium access." });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!await _rateLimiter.TryAcquireFullScanAsync(ip, email))
                return StatusCode(429, new { error = "Rate limit exceeded for full scans" });

            JsonObject scanResult;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(req.Options?.PageTimeoutSeconds ?? 1800));

            try
            {
                var scanOptions = new ScanOptions
                {
                    MaxPages = req.Options?.MaxPages ?? 0, // 0 = unlimited
                    MaxLinksPerPage = req.Options?.MaxLinksPerPage ?? 100,
                    MaxDepth = req.Options?.MaxDepth ?? 10,
                    PageTimeoutSeconds = req.Options?.PageTimeoutSeconds ?? 30,
                    IncludeSubdomains = req.Options?.IncludeSubdomains ?? false,
                    ExcludePatterns = req.Options?.ExcludePatterns ?? Array.Empty<string>(),
                    GenerateTeaser = true,
                    MaxConcurrency = req.Options?.MaxConcurrency ?? 3
                };

                scanResult = await _scanner.ScanAllPagesAsync(url, scanOptions, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(500, new { error = "Full site scan timed out." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full site accessibility scan failed for URL: {Url}", url);
                return StatusCode(500, new { error = "Full site accessibility scan failed." });
            }
            finally
            {
                _rateLimiter.ReleaseFullScan();
            }

            // Compute aggregate score across all pages
            int overallScore = ComputeOverallScore(scanResult);

            // Generate comprehensive PDF report
            string pdfUrl;
            try
            {
                pdfUrl = await GenerateFullSitePdfAsync(url, scanResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full site PDF generation failed for URL: {Url}", url);
                return StatusCode(500, new { error = "PDF generation failed." });
            }

            var teaserUrl = ExtractTeaser(scanResult);
            var totalPages = scanResult["totalPages"]?.GetValue<int>() ?? 0;

            await SaveReportAsync(scanResult, email, url, pdfUrl);

            return Ok(new
            {
                overallScore,
                pdfUrl,
                teaserUrl,
                totalPages,
                scannedAt = scanResult["scannedAt"]?.ToString(),
                pages = scanResult["pages"] // Include detailed page results
            });
        }


        private IActionResult? ValidateFullScanRequest(FullScanRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.Url)
                || !Uri.IsWellFormedUriString(req.Url.Trim(), UriKind.Absolute))
            {
                return BadRequest(new { error = "Invalid URL." });
            }

            if (string.IsNullOrWhiteSpace(req.Email)
                || !new EmailAddressAttribute().IsValid(req.Email.Trim()))
            {
                return BadRequest(new { error = "Invalid email." });
            }

            return null;
        }

        private int ComputeOverallScore(JsonObject scanResult)
        {
            if (!scanResult.TryGetPropertyValue("pages", out var pagesNode)
                || pagesNode is not JsonArray pages
                || pages.Count == 0)
            {
                throw new InvalidOperationException("Scan result did not contain any pages.");
            }

            // Calculate weighted average score across all pages
            var scores = new List<int>();
            foreach (var page in pages.Cast<JsonObject>())
            {
                if (page?["issues"] is JsonArray issues)
                {
                    scores.Add(A11yScore.From(page));
                }
            }

            return scores.Count > 0 ? (int)scores.Average() : 0;
        }

        private async Task<string> GenerateFullSitePdfAsync(string url, JsonObject scanResult)
        {
            // This would generate a comprehensive multi-page PDF report
            // You might want to create a new service method for this
            return await _pdf.GenerateAndUploadPdf(url, scanResult);
        }

        // ---------- Private helper methods ----------

        private IActionResult? ValidateRequest(ScanRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.Url)
                || !Uri.IsWellFormedUriString(req.Url.Trim(), UriKind.Absolute))
            {
                return BadRequest(new { error = "Invalid URL." });
            }

            if (string.IsNullOrWhiteSpace(req.Email)
                || !new EmailAddressAttribute().IsValid(req.Email.Trim()))
            {
                return BadRequest(new { error = "Invalid email." });
            }

            return null;
        }

        private async Task<User> GetOrCreateUserAsync(string email)
        {
            var user = await _dbContext.Users.FindAsync(email);
            if (user != null)
                return user;

            user = new User
            {
                Email = email,
                EmailVerified = false,
                FirstScan = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            try
            {
                await _dbContext.SaveChangesAsync();
                return user;
            }
            catch (DbUpdateException) // duplicate key, someone just inserted it
            {
                var existing = await _dbContext.Users.FindAsync(email);
                if (existing is null)
                    throw;

                return existing;
            }
        }

        private IActionResult? CheckVerification(User user)
        {
            if (user.Email == "derekbolyard@gmail.com") return null;
            if (!user.EmailVerified && !user.FirstScan)
            {
                return Ok(new { needVerify = true });
            }
            return null;
        }

        private async Task<IActionResult?> CheckQuotaAsync(string email)
        {
            if (email == "derekbolyard@gmail.com") return null;
            var hasQuota = await _creditManager.HasQuotaAsync(email);
            if (!hasQuota)
                return Ok(new { needPayment = true });
            return null;
        }

        private async Task UpdateFirstScanAsync(User user)
        {
            if (!user.FirstScan)
                return;

            user.FirstScan = false;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }

        private int ComputeScore(JsonObject scanResult)
        {
            if (!scanResult.TryGetPropertyValue("pages", out var pagesNode)
                || pagesNode is not JsonArray pages
                || pages.Count == 0
                || pages[0] is not JsonObject firstPage)
            {
                throw new InvalidOperationException("Scan result did not contain any pages.");
            }

            return A11yScore.From(firstPage);
        }

        private async Task<string> GeneratePdfUrlAsync(string url, JsonObject scanResult)
        {
            var firstPage = ((JsonArray)scanResult["pages"]!)[0] as JsonObject;
            if (firstPage == null)
                throw new InvalidOperationException("First page JSON missing.");

            // Assume GenerateAndUploadPdf returns a public URL
            return await _pdf.GenerateAndUploadPdf(url, firstPage);
        }

        private Teaser? ExtractTeaser(JsonObject scanResult)
        {
            if (scanResult.TryGetPropertyValue("teaser", out var teaserNode)
                && teaserNode is JsonObject teaserObj)
            {
                return teaserObj.Deserialize<Teaser>();
            }

            return null;   // or throw, or return a default Teaser – your call
        }

        private async Task SaveReportAsync(JsonObject result, string email, string siteName, string pdfUrl)
        {
            const int TOTAL_RULES = 68;
            var pages = result["pages"] as JsonArray;
            if (pages == null)
                return;

            var rules = new HashSet<string>();
            var report = new Report
            {
                Email = email,
                SiteName = siteName,
                ScanDate = DateTime.UtcNow,
                PageCount = pages.Count,
                TotalRulesTested = TOTAL_RULES,
                Status = "Completed",
                RulesPassed = 0,
                RulesFailed = 0
            };

            _dbContext.Reports.Add(report);

            foreach (JsonObject page in pages.Cast<JsonObject>())
            {
                var urlStr = page["pageUrl"]?.ToString() ?? "";
                var scanned = new ScannedUrl
                {
                    ReportId = report.ReportId,
                    Url = urlStr,
                    ScanTimestamp = DateTime.UtcNow
                };
                _dbContext.ScannedUrls.Add(scanned);

                if (page["issues"] is JsonArray issues)
                {
                    foreach (JsonObject iss in issues.Cast<JsonObject>())
                    {
                        var rule = iss["code"]?.ToString()?.Split('.')[0] ?? "";
                        rules.Add(rule);
                        var finding = new Finding
                        {
                            ReportId = report.ReportId,
                            UrlId = scanned.UrlId,
                            Issue = iss["message"]?.ToString() ?? "",
                            Rule = rule,
                            Severity = iss["type"]?.ToString() ?? "",
                            Status = "Open"
                        };
                        _dbContext.Findings.Add(finding);
                    }
                }
            }

            report.RulesFailed = rules.Count;
            report.RulesPassed = TOTAL_RULES - report.RulesFailed;

            await _dbContext.SaveChangesAsync();
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

        private async Task<bool> VerifyTurnstileAsync(string token, string ip)
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new ("secret",   _config["Turnstile:SecretKey"]),
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
    }
}
