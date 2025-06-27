using AccessLensApi.Common.Jobs;
using AccessLensApi.Common.Repositories;
using AccessLensApi.Common.Services;
using AccessLensApi.Data;
using AccessLensApi.Features.Reports.Models;
using AccessLensApi.Features.Scans.Services;
using AccessLensApi.Features.Scans.Utilities;

namespace AccessLensApi.Workers
{
    public class ScanWorkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IJobQueue<ScanJob> _jobQueue;
        private readonly ILogger<ScanWorkerService> _logger;

        public ScanWorkerService(
            IServiceProvider serviceProvider,
            IJobQueue<ScanJob> jobQueue,
            ILogger<ScanWorkerService> logger)
        {
            _serviceProvider = serviceProvider;
            _jobQueue = jobQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scan Worker Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _jobQueue.DequeueAsync(stoppingToken);
                    
                    if (job != null)
                    {
                        await ProcessScanJobAsync(job, stoppingToken);
                    }
                    else
                    {
                        // No jobs available, wait a bit before checking again
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scan worker service");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Scan Worker Service stopped");
        }

        private async Task ProcessScanJobAsync(ScanJob job, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                _logger.LogInformation("Processing scan job {JobId} for {SiteUrl}", job.Id, job.SiteUrl);

                var scanner = services.GetRequiredService<IA11yScanner>();
                var unitOfWork = services.GetRequiredService<IUnitOfWork>();
                var notificationService = services.GetRequiredService<INotificationService>();

                // Mark job as processing
                await _jobQueue.MarkJobStartedAsync(job.Id);

                // Convert job options to scan options
                var scanOptions = new Features.Scans.Models.ScanOptions
                {
                    MaxPages = job.Options.MaxPages,
                    MaxConcurrency = job.Options.MaxConcurrency,
                    GenerateTeaser = job.Options.GenerateTeaser,
                    UseSitemap = true,
                    MaxLinksPerPage = 50
                };

                // Perform the scan
                var scanResult = await scanner.ScanAllPagesAsync(job.SiteUrl, scanOptions, cancellationToken);

                // Calculate aggregated metrics from successful scans only
                var totalIssues = scanResult.Pages.Sum(p => p.Issues.Count);
                var criticalIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "critical"));
                var seriousIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "serious"));

                _logger.LogInformation("Scan completed: {Total} pages discovered, {Success} successful, {Failed} failed", 
                    scanResult.TotalPages, scanResult.SuccessfulPages, scanResult.FailedPages);

                // Create report in database
                var report = new Report
                {
                    ReportId = Guid.NewGuid(),
                    Email = job.UserEmail,
                    SiteName = job.SiteName,
                    ScanDate = scanResult.ScannedAtUtc,
                    PageCount = scanResult.SuccessfulPages,  // Only count successful pages in main report
                    RulesPassed = CalculatePassedRules(scanResult),
                    RulesFailed = totalIssues,
                    TotalRulesTested = CalculateTotalRulesTested(scanResult),
                    Status = "Completed",
                    SiteId = job.SiteId
                };

                // Add the report
                await unitOfWork.Repository<Report>().AddAsync(report);

                // Create ScannedUrl records for ALL pages (successful + failed)
                foreach (var pageScan in scanResult.PageScans)
                {
                    var scannedUrl = new Features.Reports.Models.ScannedUrl
                    {
                        UrlId = Guid.NewGuid(),
                        ReportId = report.ReportId,
                        Url = pageScan.Url,
                        ScanTimestamp = scanResult.ScannedAtUtc,
                        Title = ExtractTitleFromUrl(pageScan.Url),
                        ScanDurationMs = (int)pageScan.ScanDuration.TotalMilliseconds
                    };

                    if (pageScan.IsSuccess)
                    {
                        // Successful scan
                        scannedUrl.ScanStatus = "Success";
                        // ResponseTime could be extracted from scan duration for now
                        scannedUrl.ResponseTime = (int)pageScan.ScanDuration.TotalMilliseconds;
                    }
                    else if (pageScan.FailureInfo != null)
                    {
                        // Failed scan
                        scannedUrl.ScanStatus = ScanResultHelper.DetermineFailureStatus(pageScan.FailureInfo);
                        scannedUrl.ErrorMessage = pageScan.FailureInfo.ErrorMessage;
                        scannedUrl.HttpStatusCode = pageScan.FailureInfo.HttpStatusCode;
                        scannedUrl.ResponseTime = pageScan.FailureInfo.ResponseTime.HasValue 
                            ? (int)pageScan.FailureInfo.ResponseTime.Value.TotalMilliseconds 
                            : null;
                    }

                    await unitOfWork.Repository<Features.Reports.Models.ScannedUrl>().AddAsync(scannedUrl);

                    // Only create findings for successful scans
                    if (pageScan.IsSuccess && pageScan.Result != null)
                    {
                        foreach (var issue in pageScan.Result.Issues)
                        {
                            var finding = new Features.Reports.Models.Finding
                            {
                                FindingId = Guid.NewGuid(),
                                ReportId = report.ReportId,
                                UrlId = scannedUrl.UrlId,
                                Issue = issue.Message,
                                Rule = issue.Code,
                                Severity = MapSeverity(issue.Type),
                                Category = MapViolationToCategory(issue.Code),
                                FirstDetected = scanResult.ScannedAtUtc,
                                LastSeen = scanResult.ScannedAtUtc
                            };

                            await unitOfWork.Repository<Features.Reports.Models.Finding>().AddAsync(finding);
                        }
                    }
                }

                // Save all changes
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Handle notifications based on scan type
                await HandleScanNotificationAsync(job, report, scanResult, services, cancellationToken);

                await _jobQueue.MarkJobCompletedAsync(job.Id);
                _logger.LogInformation("Scan job {JobId} completed successfully. Report {ReportId} created with {PageCount} pages and {IssueCount} issues", 
                    job.Id, report.ReportId, scanResult.TotalPages, totalIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scan job {JobId}", job.Id);
                
                // Send failure notification
                var notificationService = services.GetRequiredService<INotificationService>();
                // TODO: Fix notification service ambiguity
                // await notificationService.NotifyScanFailedAsync(job.UserEmail, job.SiteName, ex.Message);

                await _jobQueue.MarkJobFailedAsync(job.Id, ex.Message);

                // Retry logic
                if (job.RetryCount < job.MaxRetries)
                {
                    job.RetryCount++;
                    _logger.LogInformation("Retrying job {JobId} (attempt {Retry}/{MaxRetries})", 
                        job.Id, job.RetryCount, job.MaxRetries);
                    
                    // Re-enqueue for retry after exponential backoff
                    await Task.Delay(TimeSpan.FromMinutes(Math.Pow(2, job.RetryCount - 1)), cancellationToken);
                    await _jobQueue.EnqueueAsync(job);
                }
                else
                {
                    _logger.LogError("Job {JobId} exceeded maximum retry attempts ({MaxRetries})", job.Id, job.MaxRetries);
                }
            }
        }

        private async Task<PageScanResult> ScanPageWithRetriesAsync(
            IPageScanner scanner,
            string url,
            bool captureScreenshot,
            int maxRetries = 2,
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var result = await scanner.ScanPageAsync(url, captureScreenshot, cancellationToken);
                
                if (result.IsSuccess)
                {
                    if (attempt > 0)
                        _logger.LogInformation("✓ {Url} succeeded on retry {Attempt}/{MaxRetries}", url, attempt, maxRetries);
                    return result;
                }

                // Don't retry if this is the last attempt
                if (attempt == maxRetries)
                {
                    _logger.LogWarning("✗ {Url} failed after {Attempts} attempts: {Error}", 
                        url, attempt + 1, result.FailureInfo?.ErrorMessage ?? "Unknown");
                    return result;
                }

                // Check if we should retry this failure type
                if (result.FailureInfo != null && !ScanResultHelper.ShouldRetry(result.FailureInfo, attempt, maxRetries))
                {
                    _logger.LogWarning("✗ {Url} failed with non-retryable error: {Error}", 
                        url, result.FailureInfo.ErrorMessage);
                    return result;
                }

                // Wait before retrying (exponential backoff)
                var delay = ScanResultHelper.CalculateRetryDelay(attempt + 1);
                _logger.LogInformation("⏳ {Url} failed (attempt {Attempt}), retrying in {Delay}ms: {Error}", 
                    url, attempt + 1, delay.TotalMilliseconds, result.FailureInfo?.ErrorMessage ?? "Unknown");
                
                await Task.Delay(delay, cancellationToken);
            }

            // Should never reach here, but just in case
            throw new InvalidOperationException("Retry loop completed unexpectedly");
        }

        private async Task HandleScanNotificationAsync(
            ScanJob job, 
            Report report, 
            Features.Scans.Models.A11yScanResult scanResult, 
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var notificationService = services.GetRequiredService<INotificationService>();

            switch (job.NotificationType)
            {
                case ScanNotificationType.None:
                    _logger.LogInformation("No notification configured for job {JobId}", job.Id);
                    break;

                case ScanNotificationType.Basic:
                    // TODO: Fix notification service ambiguity
                    // await notificationService.NotifyScanCompletedAsync(job.UserEmail, job.SiteName, report.ReportId.ToString());
                    break;

                case ScanNotificationType.RichWithPdf:
                    await SendRichScanNotificationAsync(job, report, scanResult, services, cancellationToken);
                    break;

                default:
                    // Fallback to basic notification
                    // TODO: Fix notification service ambiguity
                    // await notificationService.NotifyScanCompletedAsync(job.UserEmail, job.SiteName, report.ReportId.ToString());
                    break;
            }
        }

        private async Task SendRichScanNotificationAsync(
            ScanJob job,
            Report report,
            Features.Scans.Models.A11yScanResult scanResult,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            try
            {
                var notificationService = services.GetRequiredService<INotificationService>();
                var reportBuilder = services.GetRequiredService<Features.Reports.IReportBuilder>();
                var storageService = services.GetRequiredService<Storage.IStorageService>();

                // Calculate score
                var score = (int)CalculateAccessibilityScore(scanResult);

                // Generate PDF
                var accessibilityReport = ConvertToAccessibilityReport(report, scanResult);
                var html = reportBuilder.RenderHtml(accessibilityReport);
                var pdfBytes = await reportBuilder.GeneratePdfAsync(html);

                // Store PDF
                var pdfKey = $"reports/{report.ReportId}/report.pdf";
                await storageService.UploadAsync(pdfKey, System.Text.Encoding.UTF8.GetBytes(pdfBytes), cancellationToken);
                var pdfUrl = storageService.GetPresignedUrl(pdfKey, TimeSpan.FromDays(30));

                // Get teaser URL if available
                string? teaserUrl = null;
                if (scanResult.Teaser?.Url != null)
                {
                    teaserUrl = scanResult.Teaser.Url;
                }

                // Send rich notification
                await notificationService.NotifyFreeScanCompletedAsync(job.UserEmail, job.SiteName, pdfUrl, score, teaserUrl);

                _logger.LogInformation("Rich scan notification sent for job {JobId} - PDF: {PdfUrl}, Score: {Score}", 
                    job.Id, pdfUrl, score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rich scan notification for job {JobId}. Falling back to basic notification.", job.Id);
                
                // Fallback to basic notification
                var notificationService = services.GetRequiredService<INotificationService>();
                // TODO: Fix notification service ambiguity
                // await notificationService.NotifyScanCompletedAsync(job.UserEmail, job.SiteName, report.ReportId.ToString());
            }
        }

        private static Features.Core.Models.AccessibilityReport ConvertToAccessibilityReport(Report report, Features.Scans.Models.A11yScanResult scanResult)
        {
            // Convert scan result pages to accessibility model
            var pages = scanResult.Pages.Select(page => new Features.Core.Models.AccessibilityPageResult
            {
                Url = page.PageUrl,
                Issues = page.Issues.Select(issue => new Features.Core.Models.AccessibilityIssue
                {
                    Code = issue.Code,
                    Title = issue.Code,
                    Message = issue.Message,
                    Severity = issue.Type.ToUpperInvariant(),
                    Category = MapViolationToCategory(issue.Code),
                    Status = "Open",
                    InstanceCount = 1
                }).ToList()
            }).ToList();

            var scanResultModel = new Features.Core.Models.AccessibilityScanResult
            {
                ScannedAt = scanResult.ScannedAtUtc,
                SiteUrl = report.SiteName,
                DiscoveryMethod = scanResult.DiscoveryMethod,
                Pages = pages,
                TeaserImageUrl = scanResult.Teaser?.Url
            };

            return new Features.Core.Models.AccessibilityReport
            {
                SiteUrl = report.SiteName,
                ScanDate = report.ScanDate.ToString("yyyy-MM-dd"),
                ScanResult = scanResultModel
            };
        }

        private static int CalculatePassedRules(Features.Scans.Models.A11yScanResult scanResult)
        {
            // This is a simplified calculation - in reality you'd need to track rules that were tested but passed
            // For now, we'll estimate based on the assumption that each page tests ~50 rules on average
            return scanResult.TotalPages * 50 - scanResult.Pages.Sum(p => p.Issues.Count);
        }

        private static int CalculateTotalRulesTested(Features.Scans.Models.A11yScanResult scanResult)
        {
            // Estimated total rules tested across all pages
            return scanResult.TotalPages * 50;
        }

        private static double CalculateAccessibilityScore(Features.Scans.Models.A11yScanResult scanResult)
        {
            var totalIssues = scanResult.Pages.Sum(p => p.Issues.Count);
            var totalPages = scanResult.TotalPages;
            
            if (totalPages == 0) return 100.0;
            
            // Simple scoring: start at 100, subtract points for issues
            var criticalIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "critical"));
            var seriousIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "serious"));
            var moderateIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "moderate"));
            var minorIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "minor"));
            
            var penalty = (criticalIssues * 10) + (seriousIssues * 5) + (moderateIssues * 2) + (minorIssues * 1);
            var score = Math.Max(0, 100 - (penalty / totalPages));
            
            return Math.Round((double)score, 1);
        }

        private static string MapSeverity(string issueType)
        {
            return issueType.ToLowerInvariant() switch
            {
                "critical" => "Critical",
                "serious" => "Serious",
                "moderate" => "Moderate",
                "minor" => "Minor",
                _ => "Minor"
            };
        }

        private static string MapImpact(string issueType)
        {
            return issueType.ToLowerInvariant() switch
            {
                "critical" => "High",
                "serious" => "High",
                "moderate" => "Medium",
                "minor" => "Low",
                _ => "Low"
            };
        }

        private static string MapViolationToCategory(string ruleCode)
        {
            // Map axe rule codes to our categories
            return ruleCode switch
            {
                var code when code.Contains("color") => Features.Reports.Models.FindingCategories.Color,
                var code when code.Contains("keyboard") => Features.Reports.Models.FindingCategories.Keyboard,
                var code when code.Contains("focus") => Features.Reports.Models.FindingCategories.Focus,
                var code when code.Contains("image") => Features.Reports.Models.FindingCategories.Images,
                var code when code.Contains("form") => Features.Reports.Models.FindingCategories.Forms,
                var code when code.Contains("navigation") || code.Contains("landmark") => Features.Reports.Models.FindingCategories.Navigation,
                var code when code.Contains("structure") || code.Contains("heading") => Features.Reports.Models.FindingCategories.Structure,
                var code when code.Contains("video") || code.Contains("audio") => Features.Reports.Models.FindingCategories.Multimedia,
                _ => Features.Reports.Models.FindingCategories.Other
            };
        }

        private static string ExtractTitleFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.Trim('/');
                if (string.IsNullOrEmpty(path))
                    return "Home";
                
                // Convert path to a readable title
                return path.Split('/').Last()
                    .Replace("-", " ")
                    .Replace("_", " ")
                    .Replace(".html", "")
                    .Replace(".php", "")
                    .Replace(".aspx", "");
            }
            catch
            {
                return "Unknown Page";
            }
        }
    }
}
