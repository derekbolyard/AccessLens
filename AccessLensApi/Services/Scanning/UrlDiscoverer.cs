using AccessLensApi.Services.Scanning;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace AccessLensApi.Services.Scanning
{
    internal sealed class UrlDiscoverer : IUrlDiscoverer
    {
        private readonly HttpClient _http;
        private readonly ILogger<UrlDiscoverer> _log;

        public UrlDiscoverer(HttpClient http, ILogger<UrlDiscoverer> log)
        {
            _http = http;
            _log = log;
        }

        public async IAsyncEnumerable<string> DiscoverAsync(
            Uri root,
            Features.Scans.Models.ScanOptions opts,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            // A simple queue so we can recurse through nested sitemaps.
            var toExplore = new ConcurrentQueue<string>();
            var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // seed
            foreach (var c in GetRootCandidates(root))
                toExplore.Enqueue(c);

            while (toExplore.TryDequeue(out var next))
            {
                if (ct.IsCancellationRequested) yield break;

                if (next.EndsWith("robots.txt", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in await ParseRobotsAsync(next, root, ct))
                        toExplore.Enqueue(s);
                    continue;
                }

                var found = await ParseSitemapAsync(next, root, ct);
                foreach (var item in found.NestedSitemaps)
                    toExplore.Enqueue(item);

                foreach (var url in found.PageUrls)
                    if (yielded.Add(url))
                        yield return url;

                if (opts.MaxPages > 0 && yielded.Count >= opts.MaxPages)
                    break;
            }

            // fall-back to the root page if nothing was discovered
            if (yielded.Count == 0)
                yield return root.ToString();
        }

        /*──────────── parsing helpers ────────────*/

        private static IEnumerable<string> GetRootCandidates(Uri root)
        {
            yield return $"{root.Scheme}://{root.Authority}/sitemap.xml";
            yield return $"{root.Scheme}://{root.Authority}/sitemap_index.xml";
            yield return $"{root.Scheme}://{root.Authority}/sitemaps.xml";
            yield return $"{root.Scheme}://{root.Authority}/robots.txt";
        }

        private async Task<(IReadOnlyList<string> PageUrls, IReadOnlyList<string> NestedSitemaps)>
            ParseSitemapAsync(string sitemapUrl, Uri root, CancellationToken ct)
        {
            var pages = new List<string>();
            var sitemaps = new List<string>();

            try
            {
                using var res = await _http.GetAsync(sitemapUrl, ct);
                if (!res.IsSuccessStatusCode) return (pages, sitemaps);

                var xml = XDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                foreach (var loc in xml.Descendants()
                                        .Where(e => e.Name.LocalName.Equals("loc", StringComparison.OrdinalIgnoreCase))
                                        .Select(e => e.Value.Trim()))
                {
                    if (!Uri.TryCreate(loc, UriKind.Absolute, out var uri) ||
                        !SameRegisteredDomain(uri.Host, root.Host))
                        continue;

                    if (uri.AbsolutePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        sitemaps.Add(uri.ToString());     // another sitemap -> queue it
                    else
                        pages.Add(uri.ToString());        // actual page
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Failed to parse sitemap {Url}", sitemapUrl);
            }

            return (pages, sitemaps);
        }

        private async Task<IEnumerable<string>> ParseRobotsAsync(
            string robotsUrl, Uri root, CancellationToken ct)
        {
            try
            {
                using var res = await _http.GetAsync(robotsUrl, ct);
                if (!res.IsSuccessStatusCode) return Enumerable.Empty<string>();

                return (await res.Content.ReadAsStringAsync(ct))
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => l.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                        .Select(l => l.Substring(8).Trim())
                        .Where(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                                    SameRegisteredDomain(uri!.Host, root.Host))
                        .ToArray();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Failed reading robots.txt {Url}", robotsUrl);
                return Enumerable.Empty<string>();
            }
        }

        private static bool SameRegisteredDomain(string a, string b)
        {
            static string TrimWww(string h) =>
                h.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? h[4..] : h;

            return TrimWww(a).Equals(TrimWww(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
