using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AccessLensApi.Tests.Scanner
{
    public class RegexExcludeTests
    {
        // Patterns mirror what your ScanOptions might pass to ShouldExcludeUrl
        private readonly Regex[] _patterns =
        {
        new(@"#.*$",                 RegexOptions.IgnoreCase), // URL fragments
        new(@"/cart(\?|$).*",        RegexOptions.IgnoreCase), // “/cart” pages
        new(@".*[\?&]preview=true",  RegexOptions.IgnoreCase)  // preview query
    };

        [Theory]
        [InlineData("https://shop.com/#section", true)]
        [InlineData("https://shop.com/cart?id=1", true)]
        [InlineData("https://shop.com/product/123", false)]
        [InlineData("https://shop.com/page?preview=true", true)]
        [Trait("Category", "Unit")]
        public void ShouldExcludeUrl_applies_patterns(string url, bool expected)
        {
            bool actual = PrivateAccessor.ShouldExcludeUrl(url, _patterns);
            Assert.Equal(expected, actual);
        }
    }
}
