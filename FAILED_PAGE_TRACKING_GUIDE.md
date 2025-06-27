# Failed Page Tracking & Retry Implementation

## Overview 🎯

This implementation adds comprehensive tracking of **all page scan attempts** (successful and failed) with automatic retries and detailed error information. Every discovered URL is tracked in the database regardless of scan outcome.

## Key Features ✨

### 1. **Complete Page Tracking**
- **All discovered URLs** are saved to `ScannedUrls` table
- **Success and failure** details recorded
- **Scan duration** and response times tracked
- **Error categorization** for debugging

### 2. **Intelligent Retry System**
- **Automatic retries** for transient failures
- **Exponential backoff** delays (2s, 4s, 8s, etc.)
- **Smart retry logic** - doesn't retry permanent failures (4xx errors)
- **Configurable retry limits** per scan

### 3. **Enhanced Error Information**
- **Detailed failure reasons**: HttpError, Timeout, LoadFailed, ScriptError, BrowserError
- **HTTP status codes** for failed requests
- **Response times** even for failed requests
- **Error messages** with specific details

## Database Schema Changes 📊

### ScannedUrl Model Enhancements
```csharp
public class ScannedUrl
{
    // Existing fields...
    public string ScanStatus { get; set; } = "Success";      // New status values
    public string? ErrorMessage { get; set; }               // NEW: Failure details
    public int? HttpStatusCode { get; set; }                // NEW: HTTP response code
    public int? ResponseTime { get; set; }                  // Enhanced: works for failures too
    public int? ScanDurationMs { get; set; }               // NEW: Total scan time
    public int RetryCount { get; set; } = 0;               // NEW: How many retries attempted
    public int MaxRetries { get; set; } = 2;               // NEW: Retry limit
    public DateTime ScanTimestamp { get; set; }            // Enhanced timestamp
}
```

### Status Values
- **Success**: Page scanned successfully
- **HttpError**: HTTP 4xx/5xx response  
- **Timeout**: Page load timeout
- **LoadFailed**: Navigation/DNS/connection failure
- **ScriptError**: Axe script execution failed
- **BrowserError**: Playwright/browser error

## Retry Logic 🔄

### Retry Decision Matrix
| Failure Type | HTTP Code | Retry? | Reason |
|-------------|-----------|---------|---------|
| HttpError | 4xx | ❌ No | Client errors are permanent |
| HttpError | 5xx | ✅ Yes | Server errors might be temporary |
| Timeout | - | ✅ Yes | Network issues might be temporary |
| LoadFailed | - | ✅ Yes | DNS/connection might recover |
| ScriptError | - | ❌ No | Script errors are usually permanent |
| BrowserError | - | ✅ Yes | Browser errors might be temporary |

### Exponential Backoff
```csharp
Attempt 1: 2 seconds delay
Attempt 2: 4 seconds delay  
Attempt 3: 8 seconds delay
Max delay: 60 seconds
```

## API Changes 🔧

### ScanOptions Enhancement
```csharp
public class ScanOptions
{
    // Existing options...
    public int MaxRetries { get; set; } = 2;  // NEW: Configurable retries
}
```

### A11yScanResult Enhancement
```csharp
public record A11yScanResult(
    List<PageResult> Pages,                    // Successful pages only (backward compatibility)
    List<PageScanResult> PageScans,           // NEW: ALL scan attempts (success + failure)
    TeaserDto? Teaser,
    int TotalPages,                           // Total URLs discovered
    int SuccessfulPages,                      // NEW: Count of successful scans
    int FailedPages,                          // NEW: Count of failed scans
    DateTime ScannedAtUtc,
    string DiscoveryMethod
);
```

## Implementation Flow 🚀

### 1. URL Discovery
```
Site URL → Sitemap + Crawling → List of URLs to scan
```

### 2. Page Scanning with Retries
```
For each URL:
  Attempt 1 → [Success ✅ | Failure ❌]
  If failure and retryable:
    Wait 2s → Attempt 2 → [Success ✅ | Failure ❌]
    If failure and retryable:
      Wait 4s → Attempt 3 → [Success ✅ | Final Failure ❌]
```

### 3. Database Storage
```
Report (main scan info)
├── ScannedUrl (success) → Findings
├── ScannedUrl (success) → Findings  
├── ScannedUrl (failed - 404)
├── ScannedUrl (failed - timeout, retried 2x)
└── ScannedUrl (success after 1 retry) → Findings
```

### 4. Reporting
```
- Main Report.PageCount = successful pages only
- ScannedUrls table contains complete audit trail
- Failed pages visible in admin/debug views
```

## Usage Examples 💡

### Starting a Scan with Custom Retries
```csharp
var scanOptions = new ScanOptions
{
    MaxPages = 50,
    MaxConcurrency = 5,
    MaxRetries = 3,        // Try each page up to 3 times
    GenerateTeaser = true
};

var result = await scanner.ScanAllPagesAsync(siteUrl, scanOptions);
```

### Analyzing Scan Results
```csharp
Console.WriteLine($"Total URLs: {result.TotalPages}");
Console.WriteLine($"Successful: {result.SuccessfulPages}");
Console.WriteLine($"Failed: {result.FailedPages}");

// Get failure details
var failures = result.PageScans
    .Where(s => s.IsFailed)
    .GroupBy(s => s.FailureInfo?.Reason)
    .Select(g => new { Reason = g.Key, Count = g.Count() });

foreach (var failure in failures)
{
    Console.WriteLine($"{failure.Reason}: {failure.Count} pages");
}
```

### Database Queries
```sql
-- Get all failed pages for a report
SELECT Url, ScanStatus, ErrorMessage, HttpStatusCode, RetryCount
FROM ScannedUrls 
WHERE ReportId = @reportId AND ScanStatus != 'Success'
ORDER BY ScanStatus, Url;

-- Get retry statistics
SELECT 
    ScanStatus,
    AVG(RetryCount) as AvgRetries,
    MAX(RetryCount) as MaxRetries,
    COUNT(*) as PageCount
FROM ScannedUrls
GROUP BY ScanStatus;

-- Get performance metrics
SELECT 
    ScanStatus,
    AVG(ScanDurationMs) as AvgDuration,
    AVG(ResponseTime) as AvgResponseTime,
    COUNT(*) as Count
FROM ScannedUrls
GROUP BY ScanStatus;
```

## Monitoring & Analytics 📈

### Key Metrics to Track
1. **Success Rate**: `SuccessfulPages / TotalPages`
2. **Retry Effectiveness**: Pages that succeeded after retry
3. **Common Failure Types**: Most frequent error categories
4. **Performance Impact**: Scan duration vs success rate
5. **Site Reliability**: Failure patterns per domain

### Log Analysis
```
✓ https://example.com/ (5 issues)
✗ https://example.com/broken failed: HTTP 404 (non-retryable)
⏳ https://example.com/slow failed (attempt 1), retrying in 2000ms: Timeout
✓ https://example.com/slow succeeded on retry 1/2 (8 issues)
✗ https://example.com/unreliable failed after 3 attempts: Connection refused
```

## Benefits ✅

1. **Complete Visibility**: See every page that was attempted
2. **Better Debugging**: Understand why scans fail  
3. **Improved Reliability**: Automatic retries for transient issues
4. **Performance Insights**: Scan duration and timing data
5. **User Experience**: More accurate progress and completion reporting
6. **Analytics**: Rich data for improving scan success rates

## Next Steps 🚀

1. **Frontend Integration**: Show failed pages in dashboard
2. **Alerting**: Notify when failure rates exceed thresholds
3. **Advanced Retries**: Different retry strategies per failure type
4. **Caching**: Skip recently failed pages in subsequent scans
5. **Rate Limiting**: Adaptive delays based on server response

Your scan system now provides **complete transparency** into the scanning process with intelligent failure handling! 🎉
