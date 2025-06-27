using AccessLensApi.Features.Reports.Models;

namespace AccessLensApi.Features.Scans.Utilities
{
    /// <summary>
    /// Utility methods for mapping scan results to database entities
    /// </summary>
    public static class ScanResultMappingHelper
    {
        /// <summary>
        /// Maps axe issue type to database severity
        /// </summary>
        public static string MapSeverity(string issueType)
        {
            return issueType.ToLowerInvariant() switch
            {
                "critical" => "Critical",
                "serious" => "Serious", 
                "moderate" => "Moderate",
                "minor" => "Minor",
                _ => "Minor"
            };
        }

        /// <summary>
        /// Maps axe rule codes to our finding categories
        /// </summary>
        public static string MapViolationToCategory(string ruleCode)
        {
            return ruleCode switch
            {
                var code when code.Contains("color") => FindingCategories.Color,
                var code when code.Contains("keyboard") => FindingCategories.Keyboard,
                var code when code.Contains("focus") => FindingCategories.Focus,
                var code when code.Contains("image") => FindingCategories.Images,
                var code when code.Contains("form") => FindingCategories.Forms,
                var code when code.Contains("navigation") || code.Contains("landmark") => FindingCategories.Navigation,
                var code when code.Contains("structure") || code.Contains("heading") => FindingCategories.Structure,
                var code when code.Contains("video") || code.Contains("audio") => FindingCategories.Multimedia,
                _ => FindingCategories.Other
            };
        }

        /// <summary>
        /// Extracts a human-readable title from a URL
        /// </summary>
        public static string ExtractTitleFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.Trim('/');
                if (string.IsNullOrEmpty(path))
                    return "Home";
                
                // Convert path to a readable title
                return path.Split('/').Last()
                    .Replace("-", " ")
                    .Replace("_", " ")
                    .Replace(".html", "")
                    .Replace(".php", "")
                    .Replace(".aspx", "");
            }
            catch
            {
                return "Unknown Page";
            }
        }

        /// <summary>
        /// Calculates passed rules count from scan results
        /// </summary>
        public static int CalculatePassedRules(Features.Scans.Models.A11yScanResult scanResult)
        {
            // This is a simplified calculation - in practice you'd want more sophisticated logic
            var totalChecks = scanResult.Pages.Count * 50; // Estimate 50 checks per page
            var failedChecks = scanResult.Pages.Sum(p => p.Issues.Count);
            return Math.Max(0, totalChecks - failedChecks);
        }

        /// <summary>
        /// Calculates total rules tested from scan results
        /// </summary>
        public static int CalculateTotalRulesTested(Features.Scans.Models.A11yScanResult scanResult)
        {
            // Estimate based on pages scanned
            return scanResult.Pages.Count * 50; // Axe tests approximately 50 rules per page
        }

        /// <summary>
        /// Calculates accessibility score from scan results
        /// </summary>
        public static double CalculateAccessibilityScore(Features.Scans.Models.A11yScanResult scanResult)
        {
            if (!scanResult.Pages.Any())
                return 100.0;

            var totalPages = scanResult.Pages.Count;
            var criticalIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "critical"));
            var seriousIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "serious"));
            var moderateIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "moderate"));
            var minorIssues = scanResult.Pages.Sum(p => p.Issues.Count(i => i.Type == "minor"));
            
            var penalty = (criticalIssues * 10) + (seriousIssues * 5) + (moderateIssues * 2) + (minorIssues * 1);
            var score = Math.Max(0.0, 100.0 - (penalty / (double)totalPages));
            
            return Math.Round(score, 1);
        }
    }
}
