namespace AccessLensApi.Models
{
    public class ScanOptions
    {
        /// Maximum number of pages to scan (0 = unlimited)
        public int MaxPages { get; set; } = 0;

        /// Maximum links to follow per page (0 = all links)
        public int MaxLinksPerPage { get; set; } = 0;

        /// Maximum crawl depth
        public int MaxDepth { get; set; } = 10;

        /// Timeout per page in seconds
        public int PageTimeoutSeconds { get; set; } = 30;

        /// Whether to include external links (same domain only)
        public bool IncludeSubdomains { get; set; } = false;

        /// Patterns to exclude (regex)
        public string[] ExcludePatterns { get; set; } = Array.Empty<string>();

        /// Whether to generate teaser for first page
        public bool GenerateTeaser { get; set; } = true;

        /// Maximum concurrent pages to process
        public int MaxConcurrency { get; set; } = 3;

        /// Whether to use sitemap for URL discovery (in addition to crawling)
        public bool UseSitemap { get; set; } = true;
    }
}
