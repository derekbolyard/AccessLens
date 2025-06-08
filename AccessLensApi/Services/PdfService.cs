using AccessLensApi.PdfDocuments;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using QuestPDF.Fluent;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AccessLensApi.Services
{
    public class PdfService : IPdfService
    {
        private const int TOTAL_RULES_TESTED = 68;
        private readonly IStorageService _storage;
        private readonly ILogger<PdfService> _log;

        public PdfService(IStorageService storage, ILogger<PdfService> log)
        {
            _storage = storage;
            _log = log;
        }

        public async Task<string> GenerateAndUploadPdf(string siteName, JsonNode json)
        {
            var issues = LoadIssues(json);

            // --- Aggregate ---
            var summary = Aggregate(issues);

            // --- Calculate stats ---
            int uniqueFailedRules = summary.Select(x => x.Rule).Distinct().Count();
            int rulesPassed = TOTAL_RULES_TESTED - uniqueFailedRules;

            int distinctPagesCrawled = issues.Select(i => i.Page).Distinct().Count();
            var urls = issues.Select(i => i.Page).Distinct().OrderBy(u => u).ToList();

            var doc = new AccessibilityReportDocument(
                    siteName, summary, rulesPassed,
                    uniqueFailedRules, TOTAL_RULES_TESTED,
                    distinctPagesCrawled, urls);

            byte[] bytes = doc.GeneratePdf();

            /* ---------- store & return URL ---------- */
            string key = $"reports/{Guid.NewGuid()}.pdf";

            await _storage.UploadAsync(key, bytes);
            _log.LogInformation("PDF uploaded as {Key} ({Bytes} bytes)", key, bytes.Length);

            // 30-day presigned URL
            return _storage.GetPresignedUrl(key, TimeSpan.FromDays(7));
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

            // Normalize all inputs to JsonArray for consistent processing
            var normalizedData = NormalizeToArrays(raw);

            foreach (var item in normalizedData)
            {
                issues.AddRange(ParsePage(item, severityMap));
            }

            return issues;
        }

        private IEnumerable<JsonNode> NormalizeToArrays(JsonNode? raw)
        {
            return raw switch
            {
                JsonArray arr => arr,                           // Shape A: Already an array
                JsonObject obj when obj["issues"] is JsonArray => [obj],  // Shape B: Wrap object with issues array
                JsonObject obj when obj["pages"] is JsonArray pages => pages.Where(x => x != null) as IEnumerable<JsonNode>,
                _ => []                                         // Handle null or unexpected types
            };
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
