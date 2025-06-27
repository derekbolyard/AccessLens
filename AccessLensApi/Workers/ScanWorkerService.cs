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

                // Calculate overall accessibility score
                var overallScore = (int)ScanResultMappingHelper.CalculateAccessibilityScore(scanResult);

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
                    RulesPassed = ScanResultMappingHelper.CalculatePassedRules(scanResult),
                    RulesFailed = totalIssues,
                    TotalRulesTested = ScanResultMappingHelper.CalculateTotalRulesTested(scanResult),
                    Score = overallScore,  // Set the calculated score
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
                        Title = ScanResultMappingHelper.ExtractTitleFromUrl(pageScan.Url),
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
                                Severity = ScanResultMappingHelper.MapSeverity(issue.Type),
                                Category = ScanResultMappingHelper.MapViolationToCategory(issue.Code),
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
                await notificationService.NotifyScanFailedAsync(job.UserEmail, job.SiteName, ex.Message);

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
                    await notificationService.NotifyScanCompletedAsync(job.UserEmail, job.SiteName, report.ReportId.ToString());
                    break;

                case ScanNotificationType.RichWithPdf:
                    await SendRichScanNotificationAsync(job, report, scanResult, services, cancellationToken);
                    break;

                default:
                    // Fallback to basic notification
                    await notificationService.NotifyScanCompletedAsync(job.UserEmail, job.SiteName, report.ReportId.ToString());
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

                // Use the score from the report (same calculation as stored in DB)
                var score = report.Score ?? 0;

                // Generate PDF with custom storage key
                var accessibilityReport = ConvertToAccessibilityReport(report, scanResult);
                
                // Log the report data for debugging
                _logger.LogInformation("Generating PDF for report {ReportId}: SiteUrl={SiteUrl}, Score={Score}, Pages={PageCount}, Issues={IssueCount}", 
                    report.ReportId, accessibilityReport.SiteUrl, accessibilityReport.Score, 
                    accessibilityReport.Pages.Count, accessibilityReport.Pages.Sum(p => p.Issues.Count));
                
                var html = reportBuilder.RenderHtml(accessibilityReport);
                var pdfKey = $"reports/{report.ReportId}/report.pdf";
                var pdfUrl = await reportBuilder.GeneratePdfAsync(html, pdfKey);

                // Update the report with the PDF key
                report.PdfKey = pdfKey;
                
                // Save the updated report with PDF key
                var unitOfWork = services.GetRequiredService<IUnitOfWork>();
                await unitOfWork.SaveChangesAsync(cancellationToken);

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
                await notificationService.NotifyScanCompletedAsync(job.UserEmail, job.SiteName, report.ReportId.ToString());
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
                    Title = GetIssueTitleFromCode(issue.Code),
                    Message = issue.Message,
                    Severity = issue.Type.ToUpperInvariant(),
                    Target = string.Empty, // Target not available in current Issue model
                    ContextHtml = issue.ContextHtml ?? string.Empty,
                    Fix = GetFixRecommendation(issue.Code),
                    Category = ScanResultMappingHelper.MapViolationToCategory(issue.Code),
                    Status = "Open",
                    InstanceCount = 1
                }).ToList()
            }).ToList();

            // Generate top issues by grouping and counting across all pages
            var allIssues = pages.SelectMany(p => p.Issues);
            var topIssues = allIssues
                .GroupBy(i => i.Code)
                .Select(g => new Features.Core.Models.AccessibilityTopIssue
                {
                    Code = g.Key,
                    Title = g.First().Title,
                    Severity = g.First().Severity,
                    InstanceCount = g.Count(),
                    AffectedPages = g.Select(i => pages.First(p => p.Issues.Contains(i)).Url).Distinct().ToList(),
                    ExampleMessage = g.First().Message,
                    ExampleFix = g.First().Fix
                })
                .OrderByDescending(i => GetSeverityWeight(i.Severity))
                .ThenByDescending(i => i.InstanceCount)
                .Take(10)
                .ToList();

            var scanResultModel = new Features.Core.Models.AccessibilityScanResult
            {
                ScannedAt = scanResult.ScannedAtUtc,
                SiteUrl = report.SiteName,
                DiscoveryMethod = scanResult.DiscoveryMethod,
                Pages = pages,
                TopIssues = topIssues,
                TeaserImageUrl = scanResult.Teaser?.Url
            };

            var accessibilityReport = new Features.Core.Models.AccessibilityReport
            {
                SiteUrl = report.SiteName,
                ScanDate = report.ScanDate.ToString("yyyy-MM-dd"),
                Score = report.Score?.ToString() ?? "0",
                ScanResult = scanResultModel,
                ClientName = "AccessLens",
                ContactEmail = "support@accesslens.com",
                FooterText = "Generated by AccessLens - Web Accessibility Scanner",
                PrimaryColor = "#2563eb",
                SecondaryColor = "#16a34a"
            };

            // Populate from scan result to ensure all data is available
            accessibilityReport.PopulateFromScanResult();

            return accessibilityReport;
        }

        private static int GetSeverityWeight(string severity)
        {
            return severity.ToUpperInvariant() switch
            {
                "CRITICAL" => 4,
                "SERIOUS" => 3,
                "MODERATE" => 2,
                "MINOR" => 1,
                _ => 0
            };
        }

        private static string GetIssueTitleFromCode(string code)
        {
            return code switch
            {
                "color-contrast" => "Color Contrast",
                "image-alt" => "Image Alternative Text",
                "label" => "Form Labels",
                "link-name" => "Link Text",
                "heading-order" => "Heading Structure",
                "landmark-one-main" => "Main Landmark",
                "region" => "Page Regions",
                "page-has-heading-one" => "Page Heading",
                "bypass" => "Skip Links",
                "focus-order-semantics" => "Focus Order",
                "keyboard" => "Keyboard Access",
                "aria-label" => "ARIA Labels",
                "aria-labelledby" => "ARIA Labelledby",
                "aria-describedby" => "ARIA Describedby",
                "form-field-multiple-labels" => "Multiple Form Labels",
                "duplicate-id" => "Duplicate IDs",
                "meta-viewport" => "Viewport Meta Tag",
                _ => code.Replace("-", " ").Replace("_", " ")
                    .Split(' ')
                    .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower())
                    .Aggregate((a, b) => a + " " + b)
            };
        }

        private static string GetFixRecommendation(string code)
        {
            return code switch
            {
                "color-contrast" => "Ensure text has sufficient contrast against background (4.5:1 for normal text, 3:1 for large text)",
                "image-alt" => "Add descriptive alt text to images, or use alt='' for decorative images",
                "label" => "Ensure all form inputs have associated labels",
                "link-name" => "Provide descriptive text for links that explains their purpose",
                "heading-order" => "Use headings in logical order (h1, h2, h3, etc.) without skipping levels",
                "landmark-one-main" => "Include exactly one main landmark per page",
                "region" => "Organize content into meaningful regions using landmarks",
                "page-has-heading-one" => "Include exactly one h1 heading per page that describes the main content",
                "bypass" => "Provide skip links to allow keyboard users to bypass repetitive content",
                "focus-order-semantics" => "Ensure focus order follows a logical sequence",
                "keyboard" => "Ensure all interactive elements are keyboard accessible",
                "aria-label" => "Use aria-label to provide accessible names for elements",
                "aria-labelledby" => "Use aria-labelledby to reference elements that label this element",
                "aria-describedby" => "Use aria-describedby to reference elements that describe this element",
                "form-field-multiple-labels" => "Ensure form fields have only one label",
                "duplicate-id" => "Ensure all id attributes are unique on the page",
                "meta-viewport" => "Include a proper viewport meta tag for responsive design",
                _ => "Review and fix this accessibility issue according to WCAG guidelines"
            };
        }
    }
}
