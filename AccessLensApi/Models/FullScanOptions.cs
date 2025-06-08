namespace AccessLensApi.Models
{
    public class FullScanOptions
    {
        /// Maximum number of pages to scan (0 = unlimited)
        public int? MaxPages { get; set; }

        /// Maximum links to follow per page
        public int? MaxLinksPerPage { get; set; }

        /// Maximum crawl depth
        public int? MaxDepth { get; set; }

        /// Timeout per page in seconds
        public int? PageTimeoutSeconds { get; set; }

        /// Overall scan timeout in minutes
        public int? TimeoutMinutes { get; set; }

        /// Whether to include subdomains
        public bool? IncludeSubdomains { get; set; }

        /// URL patterns to exclude (regex)
        public string[]? ExcludePatterns { get; set; }

        /// Maximum concurrent pages to process
        public int? MaxConcurrency { get; set; }
    }
}
