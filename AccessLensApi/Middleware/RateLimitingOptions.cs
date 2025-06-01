namespace AccessLensApi.Middleware
{
    public class RateLimitingOptions
    {
        public int MaxUnverifiedScansPerHour { get; set; } = 10;
        public int WindowMinutes { get; set; } = 60;
    }
}
