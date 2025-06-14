using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AccessLensApi.Tests.Scanner
{
    internal static class PrivateAccessor
    {
        private static readonly Type _scanner =
            typeof(AccessLensApi.Services.A11yScanner);

        public static string NormalizeUrl(Uri uri)
            => (string)_scanner
                .GetMethod("NormalizeUrl", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { uri })!;

        public static bool IsValidSitemapUrl(string url, Uri root)
            => (bool)_scanner
                .GetMethod("IsValidSitemapUrl", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { url, root })!;

        public static bool ShouldExcludeUrl(string url, Regex[] patterns)
        => (bool)_scanner
            .GetMethod("ShouldExcludeUrl", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { url, patterns })!;
    }
}
