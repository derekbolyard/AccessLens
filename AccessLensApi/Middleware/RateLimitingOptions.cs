namespace AccessLensApi.Middleware
{
    public class RateLimitingOptions
    {
        public int WindowMinutes { get; set; } = 60;

        // Per-IP limits for starter scans
        public int MaxStarterScansPerHour { get; set; } = 3;
        public int MaxUnverifiedScansPerHour { get; set; } = 1;

        // Per-email limits for starter scans
        public int MaxScansPerEmailPerDay { get; set; } = 5;

        // Global concurrent scan limits
        public int MaxConcurrentScans { get; set; } = 5;
        public int MaxConcurrentFullScans { get; set; } = 2; // Lower limit for resource-intensive full scans

        // Full scan rate limits (more restrictive)
        public int MaxFullScansPerIpPerDay { get; set; } = 2;
        public int MaxFullScansPerEmailPerDay { get; set; } = 3;

        // Scan duration limits in seconds
        public int MaxScanDurationSeconds { get; set; } = 30;
        public int MaxFullScanDurationMinutes { get; set; } = 30;
    }
}