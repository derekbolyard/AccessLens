using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Features.Scans.Utilities;
using System.Collections.Concurrent;

namespace AccessLensApi.Features.Scans.Services
{
    /// <summary>Thin composition root that replaces the 350-line mega-class.</summary>
    public sealed class A11yScanner : IA11yScanner
    {
        private readonly IUrlDiscoverer _discoverer;
        private readonly IPageScanner _scanner;
        private readonly ITeaserGenerator _teaserGen;
        private readonly ILogger<A11yScanner> _log;

        public A11yScanner(
            IUrlDiscoverer discoverer,
            IPageScanner scanner,
            ITeaserGenerator teaserGen,
            ILogger<A11yScanner> log)
        {
            _discoverer = discoverer;
            _scanner = scanner;
            _teaserGen = teaserGen;
            _log = log;
        }

        public async Task<A11yScanResult> ScanAllPagesAsync(
            string rootUrl,
            Models.ScanOptions? opts = null,
            CancellationToken ct = default)
        {
            opts ??= new Models.ScanOptions();

            var root = new Uri(rootUrl);
            var allScans = new ConcurrentBag<PageScanResult>();
            TeaserDto? teaser = null;

            var urls = new List<string>();
            await foreach (var url in _discoverer.DiscoverAsync(root, opts, ct))
            {
                urls.Add(url);
                if (opts.MaxPages > 0 && urls.Count >= opts.MaxPages)
                    break;
            }

            _log.LogInformation("Discovered {UrlCount} URLs to scan", urls.Count);

            await Parallel.ForEachAsync(
                urls,
                new ParallelOptions { MaxDegreeOfParallelism = opts.MaxConcurrency, CancellationToken = ct },
                async (url, token) =>
                {
                    bool captureScreenshot = teaser is null && opts.GenerateTeaser;
                    var scan = await ScanPageWithRetriesAsync(url, captureScreenshot, opts.MaxRetries, token);
                    
                    // Always add to bag - even if it failed
                    allScans.Add(scan);

                    // Only process teaser for successful scans
                    if (scan.IsSuccess && teaser is null && opts.GenerateTeaser)
                        teaser = await _teaserGen.TryGenerateAsync(scan, token);

                    if (scan.IsSuccess)
                        _log.LogInformation("✓ {Url} ({Issues} issues)", url, scan.Result!.Issues.Count);
                    else
                        _log.LogWarning("✗ {Url} failed: {Error} ({Duration}ms)", url, scan.FailureInfo?.ErrorMessage ?? "Unknown", scan.ScanDuration.TotalMilliseconds);
                });

            var orderedScans = allScans.OrderBy(s => s.Url).ToList();
            var successfulScans = orderedScans.Where(s => s.IsSuccess).ToList();
            var failedScans = orderedScans.Where(s => s.IsFailed).ToList();

            // Create Pages list from successful scans only (for backward compatibility)
            var pages = successfulScans.Select(s => s.Result!).ToList();

            _log.LogInformation("Scan completed: {Total} total, {Success} successful, {Failed} failed", 
                orderedScans.Count, successfulScans.Count, failedScans.Count);

            return new A11yScanResult(
                Pages: pages,
                PageScans: orderedScans,
                Teaser: teaser,
                TotalPages: orderedScans.Count,
                SuccessfulPages: successfulScans.Count,
                FailedPages: failedScans.Count,
                ScannedAtUtc: DateTime.UtcNow,
                DiscoveryMethod: opts.UseSitemap ? "sitemap+crawling" : "crawling"
            );
        }

        public Task<A11yScanResult> ScanFivePagesAsync(
            string url, CancellationToken ct = default) =>
            ScanAllPagesAsync(url,
                new Models.ScanOptions
                {
                    MaxPages = 3,
                    MaxLinksPerPage = 30,
                    MaxConcurrency = 3,
                    UseSitemap = true
                }, ct);

        private async Task<PageScanResult> ScanPageWithRetriesAsync(
            string url,
            bool captureScreenshot,
            int maxRetries = 2,
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var result = await _scanner.ScanPageAsync(url, captureScreenshot, cancellationToken);
                
                if (result.IsSuccess)
                {
                    if (attempt > 0)
                        _log.LogInformation("✓ {Url} succeeded on retry {Attempt}/{MaxRetries}", url, attempt, maxRetries);
                    return result;
                }

                // Don't retry if this is the last attempt
                if (attempt == maxRetries)
                {
                    _log.LogWarning("✗ {Url} failed after {Attempts} attempts: {Error}", 
                        url, attempt + 1, result.FailureInfo?.ErrorMessage ?? "Unknown");
                    return result;
                }

                // Check if we should retry this failure type
                if (result.FailureInfo != null && !ScanResultHelper.ShouldRetry(result.FailureInfo, attempt, maxRetries))
                {
                    _log.LogWarning("✗ {Url} failed with non-retryable error: {Error}", 
                        url, result.FailureInfo?.ErrorMessage ?? "Unknown error");
                    return result;
                }

                // Wait before retrying (exponential backoff)
                var delay = ScanResultHelper.CalculateRetryDelay(attempt + 1);
                _log.LogInformation("⏳ {Url} failed (attempt {Attempt}), retrying in {Delay}ms: {Error}", 
                    url, attempt + 1, delay.TotalMilliseconds, result.FailureInfo?.ErrorMessage ?? "Unknown");
                
                await Task.Delay(delay, cancellationToken);
            }

            // Should never reach here, but just in case
            throw new InvalidOperationException("Retry loop completed unexpectedly");
        }
    }
}
