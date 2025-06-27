using AccessLensApi.Features.Scans.Models;
using System.Text.Json.Nodes;

namespace AccessLensApi.Features.Scans.Services
{
    /// <summary>Internal transfer object produced by <see cref="IPageScanner"/>.</summary>
    public sealed record PageScanResult(
        string Url,
        PageResult? Result,           // null if scan failed
        JsonObject? AxeJson,          // null if scan failed  
        byte[]? Screenshot,           // null if failed or not requested
        ScanFailureInfo? FailureInfo, // contains failure details if scan failed
        TimeSpan ScanDuration         // how long the scan attempt took
    )
    {
        /// <summary>True if the scan was successful</summary>
        public bool IsSuccess => Result != null && FailureInfo == null;
        
        /// <summary>True if the scan failed</summary>
        public bool IsFailed => !IsSuccess;
    };
}
