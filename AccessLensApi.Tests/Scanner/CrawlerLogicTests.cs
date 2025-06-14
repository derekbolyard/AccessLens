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
    }
}
