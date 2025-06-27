namespace AccessLensApi.Features.Scans.Models
{
    /// <summary>
    /// Contains information about why a page scan failed
    /// </summary>
    public record ScanFailureInfo(
        string Reason,              // "HttpError", "Timeout", "LoadFailed", "ScriptError", "BrowserError"
        string ErrorMessage,
        int? HttpStatusCode = null,
        TimeSpan? ResponseTime = null,
        TimeSpan? ScanDuration = null
    );

    /// <summary>
    /// Standard failure reasons for consistent categorization
    /// </summary>
    public static class ScanFailureReasons
    {
        public const string HttpError = "HttpError";         // HTTP 4xx/5xx response
        public const string Timeout = "Timeout";             // Page load timeout
        public const string LoadFailed = "LoadFailed";       // Navigation/DNS/connection failure
        public const string ScriptError = "ScriptError";     // Axe script execution failed
        public const string BrowserError = "BrowserError";   // Playwright/browser error
        public const string Unknown = "Unknown";             // Unexpected error
    }
}
