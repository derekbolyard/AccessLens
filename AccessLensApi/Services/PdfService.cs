using AccessLensApi.PdfDocuments;
using QuestPDF.Fluent;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AccessLensApi.Services
{
    public class PdfService : IPdfService
    {
        private const int TOTAL_RULES_TESTED = 68;
        public byte[] GeneratePdf(string siteName, JsonNode json)
        {
            var issues = LoadIssues(json);

            // --- Aggregate ---
            var summary = Aggregate(issues);

            // --- Calculate stats ---
            int uniqueFailedRules = summary.Select(x => x.Rule).Distinct().Count();
            int rulesPassed = TOTAL_RULES_TESTED - uniqueFailedRules;

            int distinctPagesCrawled = issues.Select(i => i.Page).Distinct().Count();

            var document = new AccessibilityReportDocument(
               siteName,
               summary,
               rulesPassed,
               uniqueFailedRules,
               TOTAL_RULES_TESTED,
               distinctPagesCrawled
           );

            return document.GeneratePdf();
        }

        private List<Issue> LoadIssues(JsonNode? raw)
        {
            var severityMap = new Dictionary<string, (string, int)>
            {
                ["critical"] = ("Critical", 0),
                ["serious"] = ("Serious", 1),
                ["error"] = ("Serious", 1),
                ["major"] = ("Major", 2),
                ["warning"] = ("Moderate", 3),
                ["minor"] = ("Minor", 4),
                ["info"] = ("Info", 5)
            };
            var issues = new List<Issue>();
            if (raw is JsonArray arr) // Shape A
            {
                foreach (var page in arr)
                    issues.AddRange(ParsePage(page, severityMap));
            }
            else if (raw?["issues"] is JsonArray) // Shape B
            {
                issues.AddRange(ParsePage(raw, severityMap));
            }
            else if (raw?["results"] is JsonObject results) // Shape C
            {
                foreach (var kv in results)
                {
                    // kv.Key is the URL, kv.Value is the array of issues
                    if (kv.Value is JsonArray issuesArray)
                    {
                        issues.AddRange(ParsePage(kv.Key, issuesArray, severityMap));
                    }
                }
            }
            return issues;
        }

        // For Shape A and B: page node contains "pageUrl"/"url" and "issues" array
        private List<Issue> ParsePage(JsonNode? page, Dictionary<string, (string, int)> severityMap)
        {
            var list = new List<Issue>();
            string pageUrl = page?["pageUrl"]?.ToString() ?? page?["url"]?.ToString() ?? "unknown";
            if (page?["issues"] is JsonArray issuesArray)
            {
                foreach (var iss in issuesArray)
                {
                    var type = iss?["type"]?.ToString()?.ToLower() ?? "";
                    var (sevLabel, sevRank) = severityMap.TryGetValue(type, out var v) ? v : ("Other", 6);
                    string rule = iss?["code"]?.ToString()?.Split('.')[0] ?? "";
                    string message = iss?["message"]?.ToString() ?? "";
                    string html = TrimHtml(iss?["context"]?.ToString() ?? "");
                    list.Add(new Issue(rule, message, html, sevLabel, sevRank, pageUrl));
                }
            }
            return list;
        }

        // For Shape C: pageUrl and issues array are provided directly
        private List<Issue> ParsePage(string pageUrl, JsonArray issuesArray, Dictionary<string, (string, int)> severityMap)
        {
            var list = new List<Issue>();
            foreach (var iss in issuesArray)
            {
                var type = iss?["type"]?.ToString()?.ToLower() ?? "";
                var (sevLabel, sevRank) = severityMap.TryGetValue(type, out var v) ? v : ("Other", 6);
                string rule = iss?["code"]?.ToString()?.Split('.')[0] ?? "";
                string message = iss?["message"]?.ToString() ?? "";
                string html = TrimHtml(iss?["context"]?.ToString() ?? "");
                list.Add(new Issue(rule, message, html, sevLabel, sevRank, pageUrl));
            }
            return list;
        }

        private string TrimHtml(string s)
            => Regex.Replace(s.Trim(), @"\s+", " ").Length > 80
                ? Regex.Replace(s.Trim(), @"\s+", " ").Substring(0, 80) + "…"
                : Regex.Replace(s.Trim(), @"\s+", " ");

        private List<(string Issue, string Rule, string Severity, int Pages, string ExampleUrl)> Aggregate(List<Issue> issues)
        {
            return issues
                .GroupBy(i => new { i.Rule, i.IssueString, i.Severity, i.SevRank })
                .OrderBy(g => g.Key.SevRank)
                .ThenByDescending(g => g.Select(x => x.Page).Distinct().Count())
                .Select(g => (
                    g.Key.IssueString,
                    g.Key.Rule,
                    g.Key.Severity,
                    Pages: g.Select(x => x.Page).Distinct().Count(),
                    ExampleUrl: g.First().Page
                ))
                .ToList();
        }

        public record Issue(string Rule, string IssueString, string HTML, string Severity, int SevRank, string Page);

    }
}
