using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using AccessLensApi.Utilities;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AccessLensApi.Services
{
    public sealed class A11yScanner : IA11yScanner, IAsyncDisposable
    {
        private readonly IBrowser _browser;
        private readonly IStorageService _storage;
        private readonly ILogger<A11yScanner> _log;
        private readonly HttpClient _httpClient;
        private const string AxeCdn = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.0/axe.min.js";

        public A11yScanner(
            IBrowser browser,
            IStorageService storage,
            ILogger<A11yScanner> log,
            HttpClient httpClient)
        {
            _browser = browser;
            _storage = storage;
            _log = log;
            _httpClient = httpClient;
        }

        public async Task<JsonObject> ScanFivePagesAsync(string rootUrl, CancellationToken cancellationToken = default)
        {
            var options = new ScanOptions
            {
                MaxPages = 5,
                MaxLinksPerPage = 30,
                MaxConcurrency = 1,
                UseSitemap = false // Keep existing behavior
            };

            return await ScanAllPagesAsync(rootUrl, options, cancellationToken);
        }

        public async Task<JsonObject> ScanAllPagesAsync(string rootUrl, ScanOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ScanOptions();

            var ctx = await _browser.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ViewportSize = new() { Width = 1280, Height = 720 }
            });

            var rootUri = new Uri(rootUrl);
            var queue = new ConcurrentQueue<(string url, int depth)>();
            var visited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var pagesResults = new ConcurrentBag<JsonObject>();
            var excludePatterns = options.ExcludePatterns.Select(p => new Regex(p, RegexOptions.IgnoreCase)).ToArray();
            string? teaserUrl = null;

            // Initialize URL discovery
            await InitializeUrlDiscovery(rootUrl, rootUri, queue, options, cancellationToken);

            var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
            var tasks = new List<Task>();

            try
            {
                while ((!queue.IsEmpty || tasks.Any()) &&
                       (options.MaxPages == 0 || pagesResults.Count < options.MaxPages))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Start new tasks for available URLs
                    while (queue.TryDequeue(out var item) &&
                           (options.MaxPages == 0 || pagesResults.Count < options.MaxPages))
                    {
                        var (url, depth) = item;

                        if (!visited.TryAdd(url, true) || depth > options.MaxDepth)
                            continue;

                        if (ShouldExcludeUrl(url, excludePatterns))
                            continue;

                        await semaphore.WaitAsync(cancellationToken);

                        var task = ProcessPageWithSemaphoreAsync(ctx, url, depth, rootUri, queue, pagesResults, options, semaphore, cancellationToken);
                        tasks.Add(task);

                        // Update teaserUrl if this is the first page and we got a result
                        if (options.GenerateTeaser && teaserUrl == null)
                        {
                            await task.ContinueWith(async t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                {
                                    var result = await t;
                                    if (result.teaserUrl != null)
                                        teaserUrl = result.teaserUrl;
                                }
                            }, TaskContinuationOptions.OnlyOnRanToCompletion);
                        }
                    }

                    // Wait for some tasks to complete
                    if (tasks.Count > 0)
                    {
                        var completed = await Task.WhenAny(tasks);
                        tasks.Remove(completed);
                    }

                    // Small delay to prevent tight loop
                    if (queue.IsEmpty && tasks.Any())
                        await Task.Delay(100, cancellationToken);
                }

                // Wait for all remaining tasks
                await Task.WhenAll(tasks);

                var sortedPages = pagesResults
                    .OrderBy(p => p["pageUrl"]?.ToString())
                    .ToArray();

                return new JsonObject
                {
                    ["pages"] = new JsonArray(sortedPages.Cast<JsonNode>().ToArray()),
                    ["teaserUrl"] = teaserUrl ?? string.Empty,
                    ["totalPages"] = sortedPages.Length,
                    ["scannedAt"] = DateTime.UtcNow.ToString("O"),
                    ["discoveryMethod"] = options.UseSitemap ? "sitemap+crawling" : "crawling"
                };
            }
            finally
            {
                await ctx.CloseAsync();
                semaphore.Dispose();
            }
        }

        private async Task<(JsonObject? pageResult, string? teaserUrl)> ProcessPageWithSemaphoreAsync(
            IBrowserContext ctx,
            string url,
            int depth,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            ConcurrentBag<JsonObject> pagesResults,
            ScanOptions options,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ProcessPageAsync(ctx, url, depth, rootUri, queue, pagesResults, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to process page: {Url}", url);
                return (null, null);
            }
            finally
            {
                semaphore.Release(); // Ensure semaphore is always released
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

        private async Task<(JsonObject? pageResult, string? teaserUrl)> ProcessPageAsync(
            IBrowserContext ctx,
            string url,
            int depth,
            Uri rootUri,
            ConcurrentQueue<(string url, int depth)> queue,
            ConcurrentBag<JsonObject> pagesResults,
            ScanOptions options,
            CancellationToken cancellationToken)
        {
            IPage? page = null;
            try
            {
                page = await ctx.NewPageAsync();
                await page.SetViewportSizeAsync(1280, 720);

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
                var axeScript = await GetAxeScriptAsync(cancellationToken);
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
                string? teaserUrl = null;
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
                    teaserUrl = _storage.GetPresignedUrl(key, TimeSpan.FromDays(7));

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

                return (pageResult, teaserUrl);
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
                var links = await page.EvaluateAsync<string[]>(@"
            Array.from(document.querySelectorAll('a[href]'))
                .map(a => a.href)
                .filter(href => href && !href.startsWith('mailto:') && !href.startsWith('tel:'))
                .slice(0, arguments[0])
        ", options.MaxLinksPerPage);

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

        private async Task<string> GetAxeScriptAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Try to get from CDN first
                var response = await _httpClient.GetAsync(AxeCdn, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to load axe-core from CDN, falling back to embedded version");
            }

            // Fallback to a minimal embedded version or throw
            throw new InvalidOperationException("Could not load axe-core script. Consider embedding axe-core as a resource.");
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

        public async ValueTask DisposeAsync() => await _browser.DisposeAsync();
    }
}
