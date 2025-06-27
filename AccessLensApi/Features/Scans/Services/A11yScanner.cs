using AccessLensApi.Features.Scans.Models;
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
            var bag = new ConcurrentBag<PageResult>();
            TeaserDto? teaser = null;

            var urls = new List<string>();
            await foreach (var url in _discoverer.DiscoverAsync(root, opts, ct))
            {
                urls.Add(url);
                if (opts.MaxPages > 0 && urls.Count >= opts.MaxPages)
                    break;
            }

            await Parallel.ForEachAsync(
                urls,
                new ParallelOptions { MaxDegreeOfParallelism = opts.MaxConcurrency, CancellationToken = ct },
                async (url, token) =>
                {
                    bool captureScreenshot = teaser is null && opts.GenerateTeaser;
                    var scan = await _scanner.ScanPageAsync(url, captureScreenshot, token);
                    if (scan is null) return;

                    bag.Add(scan.Result);

                    if (teaser is null && opts.GenerateTeaser)
                        teaser = await _teaserGen.TryGenerateAsync(scan, token);

                    _log.LogInformation("✓ {Url} ({Issues} issues)", url, scan.Result.Issues.Count);
                });

            var ordered = bag.OrderBy(p => p.PageUrl).ToList();

            return new A11yScanResult(
                Pages: ordered,
                Teaser: teaser,
                TotalPages: ordered.Count,
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
    }
}
