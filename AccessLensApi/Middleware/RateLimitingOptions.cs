namespace AccessLensApi.Middleware
{
    public class RateLimitingOptions
    {
        public int WindowMinutes { get; set; } = 60;

        // Per-IP limits
        public int MaxStarterScansPerHour { get; set; } = 3;
        public int MaxUnverifiedScansPerHour { get; set; } = 1;

        // Per-email limits
        public int MaxScansPerEmailPerDay { get; set; } = 5;

        // Global concurrent scan limit
        public int MaxConcurrentScans { get; set; } = 5;

        // Scan duration limit in seconds
        public int MaxScanDurationSeconds { get; set; } = 30;
    }
}
