using AccessLensApi.Models.ScannerDtos;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using AccessLensApi.Utilities;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace AccessLensApi.Services
{
    public sealed class A11yScanner : IA11yScanner
    {
        private readonly IBrowserProvider _provider;
        private readonly IStorageService _storage;
        private readonly ILogger<A11yScanner> _log;
        private readonly HttpClient _httpClient;
        private readonly IAxeScriptProvider _axe;

        /*────────────────────────────  ctor  ────────────────────────────*/
        public A11yScanner(
            IStorageService storage,
            ILogger<A11yScanner> log,
            HttpClient httpClient,
            IAxeScriptProvider axe,
            IBrowserProvider provider)
        {
            _storage = storage;
            _log = log;
            _httpClient = httpClient;
            _axe = axe;
            _provider = provider;
        }

        /*────────────────────────────  PUBLIC  ──────────────────────────*/

        public async Task<A11yScanResult> ScanFivePagesAsync(
            string rootUrl,
            CancellationToken ct = default)
        {
            var options = new Features.Scans.Models.ScanOptions
            {
                MaxPages = 3,
                MaxLinksPerPage = 30,
                MaxConcurrency = 3,
                UseSitemap = true
            };

            return await ScanAllPagesAsync(rootUrl, options, ct);
        }

        public async Task<A11yScanResult> ScanAllPagesAsync(
            string rootUrl,
            Features.Scans.Models.ScanOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= new Features.Scans.Models.ScanOptions();

            var browser = await _provider.GetBrowserAsync();
            var rootUri = new Uri(rootUrl);
            var queue = new ConcurrentQueue<(string url, int depth)>();
            var visited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var pagesResults = new ConcurrentBag<PageResult>();
            var excludeRx = options.ExcludePatterns
                                      .Select(p => new Regex(p, RegexOptions.IgnoreCase))
                                      .ToArray();
            Teaser? teaser = null;

            /*──── 0️⃣ shared context ────*/
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1280, Height = 720 }
            });

            var axeSrc = await _axe.GetAsync(ct);
            await ctx.AddInitScriptAsync(axeSrc);

            // block heavy assets
            await ctx.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,webp,woff2,ttf}", r => r.AbortAsync());

            /*──── 1️⃣ seed queue ─────*/
            await InitializeUrlDiscovery(rootUrl, rootUri, queue, options, ct);

            var sem = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
            var tasks = new List<Task>();

            try
            {
                while ((!queue.IsEmpty || tasks.Any()) &&
                       (options.MaxPages == 0 || pagesResults.Count < options.MaxPages))
                {
                    ct.ThrowIfCancellationRequested();

                    while (queue.TryDequeue(out var item) &&
                           (options.MaxPages == 0 || pagesResults.Count < options.MaxPages))
                    {
                        var (url, depth) = item;

                        if (!visited.TryAdd(url, true) || depth > options.MaxDepth)
                            continue;

                        if (ShouldExcludeUrl(url, excludeRx))
                            continue;

                        await sem.WaitAsync(ct);

                        _log.LogDebug("⇢ {Url} (depth {Depth})", url, depth);

                        tasks.Add(Task.Run(async () =>
                        {
                            IPage? page = null;

                            try
                            {
                                page = await ctx.NewPageAsync();
                                var (pageResult, pageTeaser) = await ProcessPageAsync(
                                    page, url, depth, rootUri, queue, pagesResults, options, ct);

                                if (options.GenerateTeaser && teaser is null &&
                                    pageTeaser?.Url is { Length: > 0 })
                                {
                                    teaser = pageTeaser;
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.LogError(ex, "Error processing page {Url}", url);
                            }
                            finally
                            {
                                if (page is not null) await page.CloseAsync();
                                sem.Release();
                            }
                        }, ct));
                    }

                    tasks.RemoveAll(t => t.IsCompleted);

                    if (queue.IsEmpty && tasks.Any())
                        await Task.Delay(50, ct);
                }

                await Task.WhenAll(tasks);

                var ordered = pagesResults.OrderBy(p => p.PageUrl).ToList();

                return new A11yScanResult(
                    Pages: ordered,
                    Teaser: teaser,
                    TotalPages: ordered.Count,
                    ScannedAtUtc: DateTime.UtcNow,
                    DiscoveryMethod: options.UseSitemap ? "sitemap+crawling" : "crawling"
                );
            }
            finally
            {
                sem.Dispose();
            }
        }

        /*──────────────────────────  INTERNALS  ─────────────────────────*/

        private async Task InitializeUrlDiscovery(
            string rootUrl,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            Features.Scans.Models.ScanOptions options,
            CancellationToken ct)
        {
            queue.Enqueue((rootUrl, 0));

            if (!options.UseSitemap) return;

            var sitemapUrls = await DiscoverFromSitemapAsync(rootUri, ct);
            _log.LogInformation("Discovered {Count} URLs from sitemap", sitemapUrls.Count);

            foreach (var url in sitemapUrls)
                queue.Enqueue((url, 0));
        }

        private async Task<List<string>> DiscoverFromSitemapAsync(
            Uri rootUri,
            CancellationToken ct)
        {
            var urls = new List<string>();
            var sitemapCandidates = new[]
            {
                $"{rootUri.Scheme}://{rootUri.Authority}/sitemap.xml",
                $"{rootUri.Scheme}://{rootUri.Authority}/sitemap_index.xml",
                $"{rootUri.Scheme}://{rootUri.Authority}/sitemaps.xml",
                $"{rootUri.Scheme}://{rootUri.Authority}/robots.txt"
            };

            foreach (var sitemapUrl in sitemapCandidates)
            {
                try
                {
                    var discovered = await ParseSitemapAsync(sitemapUrl, rootUri, ct);
                    urls.AddRange(discovered);

                    if (discovered.Any())
                    {
                        _log.LogInformation("Found sitemap at {SitemapUrl} with {Count} URLs",
                            sitemapUrl, discovered.Count);
                        break; // first successful sitemap is enough
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Could not access sitemap at {SitemapUrl}", sitemapUrl);
                }
            }

            return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<List<string>> ParseSitemapAsync(
            string sitemapUrl,
            Uri rootUri,
            CancellationToken ct)
        {
            var urls = new List<string>();

            try
            {
                // special-case robots.txt
                if (sitemapUrl.EndsWith("robots.txt", StringComparison.OrdinalIgnoreCase))
                    return await ParseRobotsTxtForSitemapsAsync(sitemapUrl, rootUri, ct);

                using var res = await _httpClient.GetAsync(sitemapUrl, ct);
                if (!res.IsSuccessStatusCode) return urls;

                var content = await res.Content.ReadAsStringAsync(ct);
                var doc = XDocument.Parse(content);

                // sitemap index?
                var sitemapElements = doc.Descendants()
                    .Where(e => e.Name.LocalName.Equals("sitemap", StringComparison.OrdinalIgnoreCase))
                    .Select(e => GetElementValue(e, "loc"))
                    .Where(loc => !string.IsNullOrEmpty(loc))
                    .ToList();

                if (sitemapElements.Any())
                {
                    foreach (var child in sitemapElements)
                    {
                        var childUrls = await ParseSitemapAsync(child!, rootUri, ct);
                        urls.AddRange(childUrls);
                    }
                }
                else
                {
                    // regular sitemap
                    var urlElements = doc.Descendants()
                        .Where(e => e.Name.LocalName.Equals("url", StringComparison.OrdinalIgnoreCase))
                        .Select(e => GetElementValue(e, "loc"))
                        .Where(loc => !string.IsNullOrEmpty(loc) && IsValidSitemapUrl(loc!, rootUri))
                        .ToList();

                    urls.AddRange(urlElements!);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to parse sitemap: {SitemapUrl}", sitemapUrl);
            }

            return urls;
        }

        private static string? GetElementValue(XElement parent, string elementName)
        {
            var element = parent.Element(parent.Name.Namespace + elementName) ??
                          parent.Element(XName.Get(elementName)) ??
                          parent.Descendants()
                                .FirstOrDefault(e => e.Name.LocalName.Equals(
                                    elementName, StringComparison.OrdinalIgnoreCase));

            return element?.Value.Trim();
        }

        private async Task<List<string>> ParseRobotsTxtForSitemapsAsync(
            string robotsUrl,
            Uri rootUri,
            CancellationToken ct)
        {
            var sitemapUrls = new List<string>();

            try
            {
                using var res = await _httpClient.GetAsync(robotsUrl, ct);
                if (!res.IsSuccessStatusCode) return sitemapUrls;

                var content = await res.Content.ReadAsStringAsync(ct);
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (!line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase)) continue;

                    var sitemapUrl = line.Substring(8).Trim();
                    if (!Uri.TryCreate(sitemapUrl, UriKind.Absolute, out var uri) ||
                        !uri.Host.Equals(rootUri.Host, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var urls = await ParseSitemapAsync(sitemapUrl, rootUri, ct);
                    sitemapUrls.AddRange(urls);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to parse robots.txt: {RobotsUrl}", robotsUrl);
            }

            return sitemapUrls;
        }

        private async Task<(PageResult? pageResult, Teaser? teaser)> ProcessPageAsync(
            IPage page,
            string url,
            int depth,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            ConcurrentBag<PageResult> pagesResults,
            Features.Scans.Models.ScanOptions options,
            CancellationToken ct)
        {
            try
            {
                await page.SetViewportSizeAsync(1280, 720);
                await page.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,webp,woff2,ttf}", r => r.AbortAsync());

                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });

                if (response?.Status >= 400)
                {
                    _log.LogWarning("Page returned {Status} for {Url}", response?.Status, url);
                    return (null, null);
                }

                var axeScript = await _axe.GetAsync(ct);
                await page.AddScriptTagAsync(new() { Content = axeScript });

                var axeResults = await page.EvaluateAsync<string>(@"
                    new Promise((resolve) => {
                        axe.run(document, (err, results) => {
                            if (err) resolve(JSON.stringify({ violations: [] }));
                            else    resolve(JSON.stringify(results));
                        });
                    });
                ");

                var pageResult = ConvertToPageResult(url, axeResults);
                pagesResults.Add(pageResult);

                /*──── teaser on first page ────*/
                Teaser? teaser = null;
                if (options.GenerateTeaser && pagesResults.Count == 1)
                {
                    var axeObj = (JsonObject)JsonNode.Parse(axeResults)!;
                    var violations = axeObj["violations"]!.AsArray();

                    int crit = violations.Count(v => v?["impact"]?.ToString() == "critical");
                    int serious = violations.Count(v => v?["impact"]?.ToString() == "serious");
                    int moderate = violations.Count(v => v?["impact"]?.ToString() == "moderate");
                    int score = A11yScore.From(axeObj);

                    (byte[] raw, bool _) =
                        await ScreenshotHelper.CaptureAsync(page, violations);
                    byte[] finalTeaser =
                        TeaserOverlay.AddOverlay(raw, score, crit, serious, moderate);

                    string key = $"teasers/{Guid.NewGuid():N}.png";
                    await _storage.UploadAsync(key, finalTeaser);

                    teaser = new Teaser(
                        Url: _storage.GetPresignedUrl(key, TimeSpan.FromDays(7)),
                        TopIssues: violations
                            .OrderBy(v => Models.ImpactPriority.Get(v?["impact"]?.ToString()))
                            .ThenBy(v => v?["id"]?.ToString())
                            .Take(5)
                            .Select(v => new TopIssue(
                                Severity: v?["impact"]?.ToString()?.ToUpperInvariant() ?? "UNKNOWN",
                                Text: v?["help"]?.ToString() ?? string.Empty))
                            .ToList()
                    );

                    _log.LogInformation(
                        "Generated teaser for first page: {Url} – Score {Score} / C:{Crit} S:{Serious} M:{Moderate}",
                        url, score, crit, serious, moderate);
                }

                /*──── link discovery ────*/
                if (depth < options.MaxDepth)
                    await DiscoverLinksFromPage(page, url, depth, rootUri, queue, options, ct);

                _log.LogInformation("Scanned page: {Url} – {IssueCount} issues",
                    url, pageResult.Issues.Count);

                return (pageResult, teaser);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing page {Url}", url);
                return (null, null);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        private async Task DiscoverLinksFromPage(
            IPage page,
            string currentUrl,
            int currentDepth,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            Features.Scans.Models.ScanOptions options,
            CancellationToken ct)
        {
            try
            {
                var links = await page.EvaluateAsync<string[]>(
                    "(maxLinks) => Array.from(document.querySelectorAll('a[href]'))" +
                    ".map(a => a.href)" +
                    ".filter(h => h && !h.startsWith('mailto:') && !h.startsWith('tel:'))" +
                    ".slice(0, maxLinks)",
                    options.MaxLinksPerPage);

                int added = 0;
                foreach (var link in links)
                {
                    if (added >= options.MaxLinksPerPage) break;

                    if (Uri.TryCreate(link, UriKind.Absolute, out var linkUri) &&
                        SameRegisteredDomain(linkUri.Host, rootUri.Host))
                    {
                        var normalized = NormalizeUrl(linkUri);
                        queue.Enqueue((normalized, currentDepth + 1));
                        added++;
                    }
                }

                _log.LogDebug("Discovered {Count} links from {Url}", added, currentUrl);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to discover links from page: {Url}", currentUrl);
            }
        }

        private static bool SameRegisteredDomain(string hostA, string hostB)
        {
            static string TrimWww(string h) =>
                h.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? h[4..] : h;

            return TrimWww(hostA).Equals(TrimWww(hostB), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldExcludeUrl(string url, Regex[] excludePatterns) =>
            excludePatterns.Any(pattern => pattern.IsMatch(url));

        private static string NormalizeUrl(Uri uri)
        {
            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty,
                Port = uri.IsDefaultPort ? -1 : uri.Port
            };

            var query = HttpUtility.ParseQueryString(uri.Query);
            var tracking = new[] { "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "ref" };
            foreach (var p in tracking) query.Remove(p);
            builder.Query = query.ToString();

            return builder.ToString();
        }

        private static bool IsValidSitemapUrl(string url, Uri rootUri)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (!uri.Host.Equals(rootUri.Host, StringComparison.OrdinalIgnoreCase)) return false;

            var path = uri.AbsolutePath.ToLowerInvariant();
            if (path.EndsWith(".xml") || path.EndsWith(".pdf") || path.EndsWith(".jpg") ||
                path.EndsWith(".png") || path.EndsWith(".gif") || path.EndsWith(".css") ||
                path.EndsWith(".js"))
                return false;

            return true;
        }

        private static PageResult ConvertToPageResult(string pageUrl, string axeJson)
        {
            var axe = (JsonObject)JsonNode.Parse(axeJson)!;
            var issues = new List<Issue>();

            foreach (var v in axe["violations"]!.AsArray())
            {
                issues.Add(new Issue(
                    Type: v!["impact"]!.GetValue<string>(),
                    Code: v!["id"]!.GetValue<string>(),
                    Message: v!["help"]!.GetValue<string>(),
                    ContextHtml: v!["nodes"]![0]!["html"]!.GetValue<string>()
                ));
            }

            return new PageResult(PageUrl: pageUrl, Issues: issues);
        }
    }
}
