using AccessLensApi.Models;
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

                        var task = ProcessPageAsync(ctx, url, depth, rootUri, queue, pagesResults, options, cancellationToken)
                            .ContinueWith(async t =>
                            {
                                semaphore.Release();

                                if (t.IsFaulted)
                                {
                                    _log.LogError(t.Exception, "Failed to process page: {Url}", url);
                                }
                                else if (t.IsCompletedSuccessfully && options.GenerateTeaser && teaserUrl == null)
                                {
                                    var result = await t;
                                    if (result.teaserUrl != null)
                                        teaserUrl = result.teaserUrl;
                                }
                            }, cancellationToken);

                        tasks.Add(task.Unwrap());
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
                $"{rootUri.Scheme}://{rootUri.Host}/sitemap.xml",
                $"{rootUri.Scheme}://{rootUri.Host}/sitemap_index.xml",
                $"{rootUri.Scheme}://{rootUri.Host}/sitemaps.xml",
                $"{rootUri.Scheme}://{rootUri.Host}/robots.txt" // Check robots.txt for sitemap references
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

            return element?.Value;
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
