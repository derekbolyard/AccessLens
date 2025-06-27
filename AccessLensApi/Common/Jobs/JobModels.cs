namespace AccessLensApi.Common.Jobs
{
    public interface IJobQueue<T>
    {
        Task EnqueueAsync(T job);
        Task<T?> DequeueAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetPendingJobsAsync();
        Task MarkJobStartedAsync(string jobId);
        Task MarkJobCompletedAsync(string jobId);
        Task MarkJobFailedAsync(string jobId, string errorMessage);
    }

    public abstract class JobBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public string? ErrorMessage { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
    }

    public enum JobStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

    public class ScanJob : JobBase
    {
        public string UserEmail { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public ScanOptions Options { get; set; } = new();
        public Guid? SiteId { get; set; }
        public ScanNotificationType NotificationType { get; set; } = ScanNotificationType.Basic;
        public string? ScanTier { get; set; } // "free", "premium", etc.
    }

    public enum ScanNotificationType
    {
        None = 0,           // No email notification
        Basic = 1,          // Simple "scan completed" notification
        RichWithPdf = 2     // Full email with PDF, score, and teaser
    }

    public class ScanOptions
    {
        public int MaxPages { get; set; } = 10;
        public int MaxConcurrency { get; set; } = 3;
        public bool GenerateTeaser { get; set; } = true;
        public bool GeneratePdf { get; set; } = true;
        public string[] UrlPatterns { get; set; } = Array.Empty<string>();
        public string[] ExcludePatterns { get; set; } = Array.Empty<string>();
    }
}
