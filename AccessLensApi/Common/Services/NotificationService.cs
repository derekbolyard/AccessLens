using AccessLensApi.Features.Core.Interfaces;

namespace AccessLensApi.Common.Services
{
    public interface INotificationService
    {
        Task NotifyScanCompletedAsync(string userEmail, string siteName, string reportId);
        Task NotifyScanFailedAsync(string userEmail, string siteName, string errorMessage);
        Task NotifyFreeScanCompletedAsync(string userEmail, string siteName, string pdfUrl, int score, string? teaserUrl = null);
    }

    public class EmailNotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(IEmailService emailService, ILogger<EmailNotificationService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task NotifyScanCompletedAsync(string userEmail, string siteName, string reportId)
        {
            try
            {
                var subject = $"Accessibility Scan Completed - {siteName}";
                var body = $@"
                    <h2>Scan Completed Successfully!</h2>
                    <p>Your accessibility scan for <strong>{siteName}</strong> has been completed.</p>
                    <p>You can view the results at: <a href='/reports/{reportId}'>View Report</a></p>
                    <p>Report ID: {reportId}</p>
                ";

                await _emailService.SendAsync(userEmail, subject, body);
                _logger.LogInformation("Scan completion notification sent to {UserEmail} for site {SiteName}", userEmail, siteName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send scan completion notification to {UserEmail}", userEmail);
            }
        }

        public async Task NotifyScanFailedAsync(string userEmail, string siteName, string errorMessage)
        {
            try
            {
                var subject = $"Accessibility Scan Failed - {siteName}";
                var body = $@"
                    <h2>Scan Failed</h2>
                    <p>Unfortunately, your accessibility scan for <strong>{siteName}</strong> could not be completed.</p>
                    <p><strong>Error:</strong> {errorMessage}</p>
                    <p>Please try again or contact support if the problem persists.</p>
                ";

                await _emailService.SendAsync(userEmail, subject, body);
                _logger.LogInformation("Scan failure notification sent to {UserEmail} for site {SiteName}", userEmail, siteName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send scan failure notification to {UserEmail}", userEmail);
            }
        }

        public async Task NotifyFreeScanCompletedAsync(string userEmail, string siteName, string pdfUrl, int score, string? teaserUrl = null)
        {
            try
            {
                // Use the existing rich email service for free scans
                await _emailService.SendScanResultEmailAsync(userEmail, pdfUrl, score, teaserUrl!);
                _logger.LogInformation("Free scan result email sent to {UserEmail} for site {SiteName} with score {Score}", userEmail, siteName, score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send free scan result email to {UserEmail}", userEmail);
            }
        }
    }

    /// <summary>
    /// Console-only notification service for development/testing
    /// </summary>
    public class ConsoleNotificationService : INotificationService
    {
        private readonly ILogger<ConsoleNotificationService> _logger;

        public ConsoleNotificationService(ILogger<ConsoleNotificationService> logger)
        {
            _logger = logger;
        }

        public Task NotifyScanCompletedAsync(string userEmail, string siteName, string reportId)
        {
            _logger.LogInformation("🎉 SCAN COMPLETED: {UserEmail} - {SiteName} - Report: {ReportId}", userEmail, siteName, reportId);
            return Task.CompletedTask;
        }

        public Task NotifyScanFailedAsync(string userEmail, string siteName, string errorMessage)
        {
            _logger.LogError("❌ SCAN FAILED: {UserEmail} - {SiteName} - Error: {ErrorMessage}", userEmail, siteName, errorMessage);
            return Task.CompletedTask;
        }

        public Task NotifyFreeScanCompletedAsync(string userEmail, string siteName, string pdfUrl, int score, string? teaserUrl = null)
        {
            _logger.LogInformation("🎉 FREE SCAN COMPLETED: {UserEmail} - {SiteName} - Score: {Score}/100 - PDF: {PdfUrl}", userEmail, siteName, score, pdfUrl);
            return Task.CompletedTask;
        }
    }
}
