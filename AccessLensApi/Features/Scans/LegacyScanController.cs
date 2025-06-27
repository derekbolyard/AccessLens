using Microsoft.AspNetCore.Mvc;
using AccessLensApi.Common;
using AccessLensApi.Common.Jobs;
using AccessLensApi.Common.Services;
using AccessLensApi.Features.Scans.Models;

namespace AccessLensApi.Features.Scans
{
    /// <summary>
    /// Legacy scan controller - marked for deprecation
    /// Use ScansController for new async scan processing
    /// </summary>
    [ApiController]
    [Route("api/legacy/[controller]")]
    [Obsolete("This controller is deprecated. Use ScansController for async scan processing.")]
    public class LegacyScanController : BaseController
    {
        private readonly IJobQueue<ScanJob> _scanJobQueue;
        private readonly ILogger<LegacyScanController> _logger;

        public LegacyScanController(
            IJobQueue<ScanJob> scanJobQueue,
            ILogger<LegacyScanController> logger) : base(null!)
        {
            _scanJobQueue = scanJobQueue;
            _logger = logger;
        }

        /// <summary>
        /// Legacy endpoint - redirects to new async scan system
        /// </summary>
        [HttpPost("full")]
        public async Task<IActionResult> StartFullScanLegacy([FromBody] FullScanRequest request)
        {
            _logger.LogWarning("Legacy scan endpoint called. Redirecting to async scan system.");

            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            // Convert legacy request to new scan job
            var scanJob = new ScanJob
            {
                UserEmail = userEmail,
                SiteUrl = request.Url,
                SiteName = new Uri(request.Url).Host,
                Options = new Common.Jobs.ScanOptions
                {
                    MaxPages = 10,
                    MaxConcurrency = 3,
                    GenerateTeaser = true,
                    GeneratePdf = true
                }
            };

            await _scanJobQueue.EnqueueAsync(scanJob);

            return Ok(new
            {
                Message = "Scan has been queued for background processing",
                JobId = scanJob.Id,
                Status = "Queued",
                DeprecationNotice = "This endpoint is deprecated. Please use POST /api/scans/start for new implementations.",
                StatusCheckUrl = $"/api/scans/status/{scanJob.Id}"
            });
        }

        /// <summary>
        /// Legacy endpoint - redirects to new async scan system
        /// </summary>
        [HttpPost("starter")]
        public async Task<IActionResult> StartStarterScanLegacy([FromBody] ScanRequest request)
        {
            _logger.LogWarning("Legacy starter scan endpoint called. Redirecting to async scan system.");

            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            // Convert to starter scan job (limited pages)
            var scanJob = new ScanJob
            {
                UserEmail = userEmail,
                SiteUrl = request.Url,
                SiteName = new Uri(request.Url).Host,
                Options = new Common.Jobs.ScanOptions
                {
                    MaxPages = 5, // Starter tier limit
                    MaxConcurrency = 2,
                    GenerateTeaser = true,
                    GeneratePdf = false
                }
            };

            await _scanJobQueue.EnqueueAsync(scanJob);

            return Ok(new
            {
                Message = "Starter scan has been queued for background processing",
                JobId = scanJob.Id,
                Status = "Queued",
                DeprecationNotice = "This endpoint is deprecated. Please use POST /api/scans/start for new implementations.",
                StatusCheckUrl = $"/api/scans/status/{scanJob.Id}"
            });
        }
    }
}
