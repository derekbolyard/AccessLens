using AccessLensApi.Common;
using AccessLensApi.Common.Jobs;
using AccessLensApi.Common.Services;
using AccessLensApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessLensApi.Features.Scans
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Authenticated")]
    public class ScansController : BaseController
    {
        private readonly IJobQueue<ScanJob> _scanJobQueue;
        private readonly ILogger<ScansController> _logger;

        public ScansController(
            ApplicationDbContext db, 
            IJobQueue<ScanJob> scanJobQueue,
            ILogger<ScansController> logger) : base(db)
        {
            _scanJobQueue = scanJobQueue;
            _logger = logger;
        }

        [HttpPost("start")]
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
}
