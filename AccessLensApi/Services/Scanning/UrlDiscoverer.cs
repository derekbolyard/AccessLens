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
            var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                root.ToString()   // always include the root page
            };

            // 1️⃣ Optional sitemap / robots.txt discovery
            if (opts.UseSitemap)
            {
                foreach (var url in await DiscoverViaSitemapAsync(root, ct))
                    if (queued.Add(url))
                        yield return url;
            }

            // 2️⃣ Fallback to just the root if nothing came back
            if (queued.Count == 1)
                yield return root.ToString();
        }

        /*────────── helpers ──────────*/

        private async Task<IReadOnlyCollection<string>> DiscoverViaSitemapAsync(
            Uri root, CancellationToken ct)
        {
            var list = new List<string>();
            var candidates = new[]
            {
                $"{root.Scheme}://{root.Authority}/sitemap.xml",
                $"{root.Scheme}://{root.Authority}/sitemap_index.xml",
                $"{root.Scheme}://{root.Authority}/sitemaps.xml",
                $"{root.Scheme}://{root.Authority}/robots.txt"
            };

            foreach (var url in candidates)
            {
                try
                {
                    if (url.EndsWith("robots.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        list.AddRange(await ParseRobotsAsync(url, root, ct));
                        if (list.Count > 0) break;
                        continue;
                    }

                    using var res = await _http.GetAsync(url, ct);
                    if (!res.IsSuccessStatusCode) continue;

                    var xml = XDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                    var nodes = xml.Descendants()
                                   .Where(e => e.Name.LocalName.Equals("loc", StringComparison.OrdinalIgnoreCase))
                                   .Select(e => e.Value.Trim())
                                   .Where(v => Uri.TryCreate(v, UriKind.Absolute, out var u) &&
                                               SameRegisteredDomain(u!.Host, root.Host));

                    list.AddRange(nodes);
                    if (list.Count > 0) break;     // first valid sitemap is enough
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Could not parse sitemap: {Url}", url);
                }
            }

            return list;
        }

        private async Task<IEnumerable<string>> ParseRobotsAsync(
            string robotsUrl, Uri root, CancellationToken ct)
        {
            var urls = new List<string>();

            try
            {
                using var res = await _http.GetAsync(robotsUrl, ct);
                if (!res.IsSuccessStatusCode) return urls;

                var lines = (await res.Content.ReadAsStringAsync(ct))
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (!line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase)) continue;

                    var siteUrl = line[8..].Trim();
                    if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) ||
                        !SameRegisteredDomain(uri.Host, root.Host))
                        continue;

                    // recurse once (don’t go crazy deep)
                    urls.AddRange(await DiscoverViaSitemapAsync(uri, ct));
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Failed reading robots.txt {Url}", robotsUrl);
            }

            return urls;
        }

        private static bool SameRegisteredDomain(string a, string b)
        {
            static string ClipWww(string h) =>
                h.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? h[4..] : h;

            return ClipWww(a).Equals(ClipWww(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
