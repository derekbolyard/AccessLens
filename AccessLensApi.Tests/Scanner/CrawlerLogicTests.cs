using AccessLensApi.Features.Scans.Models;
using RichardSzalay.MockHttp;

namespace AccessLensApi.Tests.Scanner
{

    public class CrawlerLogicTests
    {
        [Theory]
        [InlineData("https://foo.com/page?utm_source=x&utm_campaign=y#sec",
                    "https://foo.com/page")]
        [InlineData("https://foo.com/index.html",
                    "https://foo.com/index.html")]
        public void NormalizeUrl_sanitizes_tracking(string rawUrl, string expected)
        {
            var uri = new Uri(rawUrl);
            var actual = PrivateAccessor.NormalizeUrl(uri);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("https://foo.com/page.pdf", false)]
        [InlineData("https://bar.com/page", false)] // different host
        [InlineData("https://foo.com/blog", true)]
        public void IsValidSitemapUrl_filters_correctly(string url, bool expected)
            => Assert.Equal(expected,
                PrivateAccessor.IsValidSitemapUrl(
                    url, new Uri("https://foo.com")));

        [Fact(DisplayName = "Crawler honors sitemap when UseSitemap=true")]
        [Trait("Category", "Integration")]
        public async Task Crawler_reads_sitemap()
        {
            /* robots.txt advertises sitemap; sitemap lists 2 pages */
            var mock = new MockHttpMessageHandler();

            mock.When("https://example.com/robots.txt")
                .Respond("text/plain", "Sitemap: https://example.com/sitemap.xml");

            mock.When("https://example.com/sitemap.xml")
                .Respond("application/xml",
                    @"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
                     <url><loc>https://example.com/index.html</loc></url>
                     <url><loc>https://example.com/pricing.html</loc></url>
                  </urlset>");

            mock.When("https://example.com/index.html")
                .Respond("text/html", "<h1>Home</h1>");

            mock.When("https://example.com/pricing.html")
                .Respond("text/html", "<h1>Pricing</h1>");

            mock.When("*cdnjs.cloudflare.com*")
                .Respond("application/javascript", "/* axe-core */");

            var opts = new ScanOptions
            {
                UseSitemap = true,
                MaxPages = 10,
                MaxDepth = 0          // crawl should rely **only** on sitemap
            };

            var scanner = ScannerFactory.CreateScanner(mock.ToHttpClient());
            var json = await scanner.ScanAllPagesAsync("https://example.com", opts);

            var pages = json["pages"]!.AsArray()
                          .Select(p => p!["pageUrl"]!.GetValue<string>())
                          .Order();

            Assert.Equal(new[]
            {
            "https://example.com/index.html",
            "https://example.com/pricing.html"
            }, pages);
            Assert.Equal("sitemap+crawling", json["discoveryMethod"]!.GetValue<string>());
        }

        [Fact(DisplayName = "Crawler traverses same-site links up to MaxDepth")]
        [Trait("Category", "Integration")]
        public async Task Crawler_discovers_expected_internal_pages()
        {
            /*
             *  root.html   → <a href="/about.html">
             *              → <a href="https://evil.com/">  (should be ignored)
             *  about.html  → <a href="/contact.html">
             */

            var mock = new MockHttpMessageHandler();

            mock.When("https://mysite.com/root.html")
                .Respond("text/html",
                    @"<a href=""/about.html"">About</a>
                  <a href=""https://evil.com/"">Off-site</a>");

            mock.When("https://mysite.com/about.html")
                .Respond("text/html",
                    @"<a href=""/contact.html"">Contact</a>");

            mock.When("https://mysite.com/contact.html")
                .Respond("text/html", "<h1>Contact</h1>");

            mock.When("*cdnjs.cloudflare.com*")
                .Respond("application/javascript", "/* axe-core */");

            var scanOpts = new ScanOptions
            {
                MaxPages = 10,
                MaxDepth = 1,   // root (depth 0) + about (depth 1); contact should NOT be crawled
                MaxLinksPerPage = 10
            };

            var scanner = ScannerFactory.CreateScanner(mock.ToHttpClient());
            var json = await scanner.ScanAllPagesAsync("https://mysite.com/root.html", scanOpts);

            var crawled = json["pages"]!.AsArray()
                              .Select(p => p!["pageUrl"]!.GetValue<string>())
                              .Order();

            Assert.Equal(new[]
            {
            "https://mysite.com/about.html",
            "https://mysite.com/root.html"
            }, crawled);
        }
    }
}
