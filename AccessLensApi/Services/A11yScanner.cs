using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Models;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using AccessLensApi.Utilities;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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

        public async Task<JsonObject> ScanFivePagesAsync(string rootUrl, CancellationToken cancellationToken = default)
        {
            var options = new ScanOptions
            {
                MaxPages = 3,
                MaxLinksPerPage = 30,
                MaxConcurrency = 3,
                UseSitemap = true // Keep existing behavior
            };

            return await ScanAllPagesAsync(rootUrl, options, cancellationToken);
        }

        public async Task<JsonObject> ScanAllPagesAsync(
    string rootUrl,
    ScanOptions? options = null,
    CancellationToken ct = default)
        {
            options ??= new ScanOptions();

            var browser = await _provider.GetBrowserAsync();
            var rootUri = new Uri(rootUrl);
            var queue = new ConcurrentQueue<(string url, int depth)>();
            var visited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var pagesResults = new ConcurrentBag<JsonObject>();
            var excludeRx = options.ExcludePatterns
                                      .Select(p => new Regex(p, RegexOptions.IgnoreCase))
                                      .ToArray();
            Teaser? teaser = null;

            // ── 0️⃣  shared incognito context ────────────────────────────────────────────
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1280, Height = 720 }
            });

            var axeSrc = await _axe.GetAsync(ct);
            await ctx.AddInitScriptAsync(axeSrc);

            // block heavy assets once (applies to all future pages)
            await ctx.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,webp,woff2,ttf}", r => r.AbortAsync());

            // ── 1️⃣  seed the crawl queue ────────────────────────────────────────────────
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

                        _log.LogDebug("⇢ {Url}  (depth {Depth})", url, depth);

                        tasks.Add(Task.Run(async () =>
                        {
                            IPage? page = null;

                            try
                            {
                                page = await ctx.NewPageAsync();
                                var (pageResult, pageTeaser) = await ProcessPageAsync(
                                    page, url, depth, rootUri, queue, pagesResults, options, ct);

                                if (options.GenerateTeaser && teaser is null &&
                                    !string.IsNullOrEmpty(pageTeaser?.Url))
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

                    // drain completed tasks to keep the list small
                    tasks.RemoveAll(t => t.IsCompleted);

                    if (queue.IsEmpty && tasks.Any())
                        await Task.Delay(50, ct);
                }

                await Task.WhenAll(tasks);

                var sorted = pagesResults.OrderBy(p => p["pageUrl"]?.ToString())
                                         .Cast<JsonNode>()
                                         .ToArray();

                return new JsonObject
                {
                    ["pages"] = new JsonArray(sorted),
                    ["teaser"] = JsonSerializer.SerializeToNode(teaser)!,
                    ["totalPages"] = sorted.Length,
                    ["scannedAt"] = DateTime.UtcNow.ToString("O"),
                    ["discoveryMethod"] = options.UseSitemap ? "sitemap+crawling" : "crawling"
                };
            }
            finally
            {
                sem.Dispose();
            }
        }

        private async Task InitializeUrlDiscovery(
            string rootUrl,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            ScanOptions options,
            CancellationToken cancellationToken)
        {
            // Always start with the root URL
            queue.Enqueue((rootUrl, 0));

            // Try to discover URLs from sitemap if enabled
            if (options.UseSitemap)
            {
                var sitemapUrls = await DiscoverFromSitemapAsync(rootUri, cancellationToken);
                _log.LogInformation("Discovered {Count} URLs from sitemap", sitemapUrls.Count);

                foreach (var url in sitemapUrls)
                {
                    queue.Enqueue((url, 0)); // Sitemap URLs start at depth 0
                }
            }
        }

        private async Task<List<string>> DiscoverFromSitemapAsync(Uri rootUri, CancellationToken cancellationToken)
        {
            var urls = new List<string>();
            var sitemapUrls = new[]
            {
                 $"{rootUri.Scheme}://{rootUri.Authority}/sitemap.xml",
                 $"{rootUri.Scheme}://{rootUri.Authority}/sitemap_index.xml",
                 $"{rootUri.Scheme}://{rootUri.Authority}/sitemaps.xml",
                 $"{rootUri.Scheme}://{rootUri.Authority}/robots.txt"
            };

            foreach (var sitemapUrl in sitemapUrls)
            {
                try
                {
                    var discoveredUrls = await ParseSitemapAsync(sitemapUrl, rootUri, cancellationToken);
                    urls.AddRange(discoveredUrls);

                    if (discoveredUrls.Any())
                    {
                        _log.LogInformation("Found sitemap at {SitemapUrl} with {Count} URLs", sitemapUrl, discoveredUrls.Count);
                        break; // Use first successful sitemap
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Could not access sitemap at {SitemapUrl}", sitemapUrl);
                }
            }

            return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<List<string>> ParseSitemapAsync(string sitemapUrl, Uri rootUri, CancellationToken cancellationToken)
        {
            var urls = new List<string>();

            try
            {
                // Handle robots.txt specially
                if (sitemapUrl.EndsWith("robots.txt"))
                {
                    return await ParseRobotsTxtForSitemapsAsync(sitemapUrl, rootUri, cancellationToken);
                }

                using var response = await _httpClient.GetAsync(sitemapUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return urls;

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = XDocument.Parse(content);

                // Handle sitemap index files - look for any element named "sitemap" regardless of namespace
                var sitemapElements = doc.Descendants()
                    .Where(e => e.Name.LocalName.Equals("sitemap", StringComparison.OrdinalIgnoreCase))
                    .Select(e => GetElementValue(e, "loc"))
                    .Where(loc => !string.IsNullOrEmpty(loc))
                    .ToList();

                if (sitemapElements.Any())
                {
                    // This is a sitemap index - recursively parse individual sitemaps
                    foreach (var childSitemapUrl in sitemapElements)
                    {
                        var childUrls = await ParseSitemapAsync(childSitemapUrl!, rootUri, cancellationToken);
                        urls.AddRange(childUrls);
                    }
                }
                else
                {
                    // This is a regular sitemap - extract URLs
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
            // First try with the same namespace as the parent
            var element = parent.Element(parent.Name.Namespace + elementName);

            // If not found, try with no namespace
            if (element == null)
            {
                element = parent.Element(XName.Get(elementName));
            }

            // If still not found, search descendants with local name match
            if (element == null)
            {
                element = parent.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
            }

            return element?.Value.Trim();
        }

        private async Task<List<string>> ParseRobotsTxtForSitemapsAsync(string robotsUrl, Uri rootUri, CancellationToken cancellationToken)
        {
            var sitemapUrls = new List<string>();

            try
            {
                using var response = await _httpClient.GetAsync(robotsUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return sitemapUrls;

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                    {
                        var sitemapUrl = trimmedLine.Substring(8).Trim();
                        if (Uri.TryCreate(sitemapUrl, UriKind.Absolute, out var uri) &&
                            uri.Host.Equals(rootUri.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            var urls = await ParseSitemapAsync(sitemapUrl, rootUri, cancellationToken);
                            sitemapUrls.AddRange(urls);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to parse robots.txt: {RobotsUrl}", robotsUrl);
            }

            return sitemapUrls;
        }

        private async Task<(JsonObject? pageResult, Teaser? teaser)> ProcessPageAsync(
            IPage page,
            string url,
            int depth,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            ConcurrentBag<JsonObject> pagesResults,
            ScanOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                await page.SetViewportSizeAsync(1280, 720);
                // Block fonts & images
                await page.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,webp,woff2,ttf}", r => r.AbortAsync());

                // Navigate to the page
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });

                if (response?.Status >= 400)
                {
                    _log.LogWarning("Page returned {Status} for {Url}", response.Status, url);
                    return (null, null);
                }

                // Inject axe-core script
                var axeScript = await _axe.GetAsync(cancellationToken);
                await page.AddScriptTagAsync(new() { Content = axeScript });

                // Run axe accessibility scan
                var axeResults = await page.EvaluateAsync<string>(@"
                    new Promise((resolve) => {
                        axe.run(document, (err, results) => {
                            if (err) {
                                resolve(JSON.stringify({ violations: [] }));
                            } else {
                                resolve(JSON.stringify(results));
                            }
                        });
                    })
                ");

                // Convert to your desired format
                var pageResult = ConvertToShapeA(url, axeResults);
                pagesResults.Add(pageResult);

                // ── 2️⃣  teaser on FIRST page ─────────────────────────────────
                var teaser = new Teaser();
                if (options.GenerateTeaser && pagesResults.Count == 1) // First page only
                {
                    var axeObj = (JsonObject)JsonNode.Parse(axeResults)!;
                    var violations = axeObj["violations"]!.AsArray();

                    int crit = violations.Count(v => v?["impact"]?.ToString() == "critical");
                    int seri = violations.Count(v => v?["impact"]?.ToString() == "serious");
                    int moderate = violations.Count(v => v?["impact"]?.ToString() == "moderate");
                    int score = A11yScore.From(axeObj);

                    /* ---- capture (crop if possible) ---- */
                    (byte[] raw, bool zoomed) = await ScreenshotHelper.CaptureAsync(page, violations);

                    /* ---- overlay bar + circles ---- */
                    byte[] finalTeaser = TeaserOverlay.AddOverlay(raw, score, crit, seri, moderate);

                    /* ---- upload & URL ---- */
                    string key = $"teasers/{Guid.NewGuid()}.png";
                    await _storage.UploadAsync(key, finalTeaser);
                    teaser.Url = _storage.GetPresignedUrl(key, TimeSpan.FromDays(7));
                    teaser.TopIssues = violations
                        .OrderBy(v => ImpactPriority.Get(v?["impact"]?.ToString()))
                        .ThenBy(v => v?["id"]?.ToString())
                        .Take(5)
                        .Select(v => { 
                            var tf = new TopIssue();
                            tf.Severity = v?["impact"]?.ToString()?.ToUpperInvariant() ?? "UNKNOWN";
                            tf.Text = v?["help"]?.ToString() ?? string.Empty;
                            return tf;
                        })
                        .ToList();

                    _log.LogInformation("Generated teaser for first page: {Url} - Score: {Score}, Critical: {Critical}, Serious: {Serious}, Moderate: {Moderate}",
                        url, score, crit, seri, moderate);
                }

                // Discover new links if within depth limit
                if (depth < options.MaxDepth)
                {
                    await DiscoverLinksFromPage(page, url, depth, rootUri, queue, options, cancellationToken);
                }

                _log.LogInformation("Scanned page: {Url} - Found {IssueCount} issues",
                    url, pageResult["issues"]?.AsArray().Count ?? 0);

                return (pageResult, teaser);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing page: {Url}", url);
                return (null, null);
            }
            finally
            {
                if (page != null)
                {
                    await page.CloseAsync();
                }
            }
        }

        private async Task DiscoverLinksFromPage(
            IPage page,
            string currentUrl,
            int currentDepth,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            ScanOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                // Extract all links from the page
                var links = await page.EvaluateAsync<string[]>(
                    "(maxLinks) => Array.from(document.querySelectorAll('a[href]'))" +
                    ".map(a => a.href)" +
                    ".filter(href => href && !href.startsWith('mailto:') && !href.startsWith('tel:'))" +
                    ".slice(0, maxLinks)",
                    options.MaxLinksPerPage);

                var addedCount = 0;
                foreach (var link in links)
                {
                    if (addedCount >= options.MaxLinksPerPage)
                        break;

                    if (Uri.TryCreate(link, UriKind.Absolute, out var linkUri) &&
                        SameRegisteredDomain(linkUri.Host, rootUri.Host))
                    {
                        var normalizedUrl = NormalizeUrl(linkUri);
                        queue.Enqueue((normalizedUrl, currentDepth + 1));
                        addedCount++;
                    }
                }

                _log.LogDebug("Discovered {Count} links from {Url}", addedCount, currentUrl);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to discover links from page: {Url}", currentUrl);
            }
        }

        private static bool SameRegisteredDomain(string hostA, string hostB)
        {
            string trim(string h) => h.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? h.Substring(4)
                : h;

            return trim(hostA).Equals(trim(hostB), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldExcludeUrl(string url, Regex[] excludePatterns)
        {
            if (excludePatterns.Length == 0)
                return false;

            return excludePatterns.Any(pattern => pattern.IsMatch(url));
        }

        private static string NormalizeUrl(Uri uri)
        {
            // Remove fragment and common query parameters that don't change content
            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty,
                Port = uri.IsDefaultPort ? -1 : uri.Port
            };

            // Optionally remove common tracking parameters
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var paramsToRemove = new[] { "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "ref" };

            foreach (var param in paramsToRemove)
            {
                query.Remove(param);
            }

            builder.Query = query.ToString();
            return builder.ToString();
        }

        private static bool IsValidSitemapUrl(string url, Uri rootUri)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            // Only include URLs from the same domain
            if (!uri.Host.Equals(rootUri.Host, StringComparison.OrdinalIgnoreCase))
                return false;

            // Exclude obvious non-HTML content
            var path = uri.AbsolutePath.ToLowerInvariant();
            if (path.EndsWith(".xml") || path.EndsWith(".pdf") || path.EndsWith(".jpg") ||
                path.EndsWith(".png") || path.EndsWith(".gif") || path.EndsWith(".css") ||
                path.EndsWith(".js"))
                return false;

            return true;
        }

        private static JsonObject ConvertToShapeA(string pageUrl, string axeJson)
        {
            var axe = (JsonObject)JsonNode.Parse(axeJson)!;
            var issuesArr = new JsonArray();

            foreach (var v in axe["violations"]!.AsArray())
            {
                issuesArr.Add(new JsonObject
                {
                    ["type"] = v!["impact"]!.GetValue<string>(),        // critical, serious…
                    ["code"] = v!["id"]!.GetValue<string>(),            // e.g. color-contrast
                    ["message"] = v!["help"]!.GetValue<string>(),
                    ["context"] = v!["nodes"]![0]!["html"]!.GetValue<string>()
                });
            }

            return new JsonObject
            {
                ["pageUrl"] = pageUrl,
                ["issues"] = issuesArr
            };
        }
    }
}
