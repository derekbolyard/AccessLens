using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Features.Scans.Services;

namespace AccessLensApi.Features.Scans.Utilities
{
    /// <summary>
    /// Utility methods for working with scan results and failures
    /// </summary>
    public static class ScanResultHelper
    {
        /// <summary>
        /// Maps a ScanFailureInfo to a database-friendly status string
        /// </summary>
        public static string DetermineFailureStatus(ScanFailureInfo failureInfo)
        {
            return failureInfo.Reason switch
            {
                ScanFailureReasons.HttpError => "HttpError",
                ScanFailureReasons.Timeout => "Timeout", 
                ScanFailureReasons.LoadFailed => "LoadFailed",
                ScanFailureReasons.ScriptError => "ScriptError",
                ScanFailureReasons.BrowserError => "BrowserError",
                ScanFailureReasons.Unknown => "Unknown",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Determines if a page scan should be retried based on the failure reason
        /// </summary>
        public static bool ShouldRetry(ScanFailureInfo failureInfo, int currentRetryCount, int maxRetries)
        {
            if (currentRetryCount >= maxRetries)
                return false;

            // Don't retry permanent failures
            return failureInfo.Reason switch
            {
                ScanFailureReasons.HttpError when failureInfo.HttpStatusCode >= 400 && failureInfo.HttpStatusCode < 500 => false, // 4xx are permanent
                ScanFailureReasons.HttpError when failureInfo.HttpStatusCode >= 500 => true,  // 5xx might be temporary
                ScanFailureReasons.Timeout => true,        // Network timeouts might be temporary
                ScanFailureReasons.LoadFailed => true,     // DNS/connection issues might be temporary
                ScanFailureReasons.ScriptError => false,   // Script errors are usually permanent
                ScanFailureReasons.BrowserError => true,   // Browser errors might be temporary
                ScanFailureReasons.Unknown => true,        // Unknown errors - worth retrying
                _ => false
            };
        }

        /// <summary>
        /// Calculates the delay before retrying a failed scan (exponential backoff)
        /// </summary>
        public static TimeSpan CalculateRetryDelay(int retryCount)
        {
            var baseDelaySeconds = 2; // Start with 2 seconds
            var delaySeconds = Math.Pow(baseDelaySeconds, retryCount);
            return TimeSpan.FromSeconds(Math.Min(delaySeconds, 60)); // Cap at 60 seconds
        }
    }
}
