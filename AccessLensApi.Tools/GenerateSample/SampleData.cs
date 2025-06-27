// SampleData.cs
using AccessLensApi.Features.Core.Models;

namespace GenerateSample
{


    public static class SampleData
    {
        public static AccessibilityReport DemoReport()
        {
            var homeIssues = new List<AccessibilityIssue>
        {
            Issue("color-contrast", "Low text/background contrast",
                  "The gray text on white background fails WCAG 2.2 AA 4.5:1 contrast.",
                  "CRITICAL",
                  ".hero-tagline",
                  "<h1 class=\"hero-tagline\">Grow your brand</h1>",
                  "Increase the text colour to #000000, or darken it until contrast ≥ 4.5:1.",
                  "Colour & Contrast"),

            Issue("image-alt", "Missing alternative text",
                  "Decorative image lacks alt attribute.",
                  "SERIOUS",
                  "img[src*=\"/banner/coffee.jpg\"]",
                  "<img src=\"/banner/coffee.jpg\">",
                  "Add alt=\"\" if decorative, or alt=\"Barista pouring coffee\" if informational.",
                  "Images"),

            Issue("label", "Form field has no label",
                  "Search form input is missing an associated label.",
                  "SERIOUS",
                  "form.search input[name=q]",
                  "<input name=\"q\" placeholder=\"Search…\">",
                  "Add <label for=\"q\">Search</label> before the input.",
                  "Forms"),

            Issue("heading-order", "Skipped heading level",
                  "Found <h4> immediately after <h2>.",
                  "MINOR",
                  "h4.section-tagline",
                  "<h4 class=\"section-tagline\">Trusted by 500+ clients</h4>",
                  "Change to <h3> or re-nest headings correctly.",
                  "Structure")
        };

            var aboutIssues = new List<AccessibilityIssue>
        {
            Issue("html-has-lang", "Language attribute missing",
                  "<html> element lacks a lang attribute.",
                  "CRITICAL",
                  "html",
                  "<html>",
                  "Add lang=\"en\" (or appropriate language code) to <html>.",
                  "Metadata"),

            Issue("link-name", "Link has no accessible name",
                  "Social icon link contains no text or aria-label.",
                  "MODERATE",
                  "a[href^=\"https://twitter.com\"]",
                  "<a href=\"https://twitter.com/agency\"></a>",
                  "Add aria-label=\"Twitter\" or visible text.",
                  "Links")
        };

            var contactIssues = new List<AccessibilityIssue>
        {
            Issue("duplicate-id", "Duplicate element id",
                  "Multiple elements use id=\"submit-btn\".",
                  "CRITICAL",
                  "#submit-btn",
                  "<button id=\"submit-btn\">Send</button>",
                  "Ensure each id value is unique.",
                  "DOM"),

            Issue("aria-required-children", "Accordion missing required children",
                  "Role=\"tablist\" lacks role=\"tab\" children.",
                  "SERIOUS",
                  "div[role=tablist]",
                  "<div role=\"tablist\">…</div>",
                  "Wrap each toggle button in role=\"tab\" and role=\"tabpanel\".",
                  "ARIA")
        };

            var pages = new List<AccessibilityPageResult>
        {
            Page("https://agency-xyz.com/",          homeIssues),
            Page("https://agency-xyz.com/about",     aboutIssues),
            Page("https://agency-xyz.com/contact",   contactIssues)
        };

            // Aggregate top-issue summary
            var topIssues = pages
                .SelectMany(p => p.Issues)
                .GroupBy(i => i.Code)
                .Select(g => new AccessibilityTopIssue
                {
                    Code = g.Key,
                    Title = g.First().Title,
                    Severity = g.First().Severity,
                    InstanceCount = g.Sum(x => x.InstanceCount),
                    AffectedPages = g.Select(x => x.Target).Distinct().Take(5).ToList(),
                    ExampleMessage = g.First().Message,
                    ExampleFix = g.First().Fix
                })
                .OrderByDescending(t => t.InstanceCount)
                .ThenBy(t => t.Severity)
                .Take(6)
                .ToList();

            return new AccessibilityReport
            {
                WhiteLabel = false,
                ClientName = "Agency XYZ",
                ClientLogoUrl = "https://cdn.accesslens.com/samples/agencyxyz-logo.png",
                PrimaryColor = "#2563eb",
                SecondaryColor = "#16a34a",

                ScanResult = new AccessibilityScanResult
                {
                    ScannedAt = DateTime.UtcNow.AddDays(-2),
                    SiteUrl = "https://agency-xyz.com",
                    DiscoveryMethod = "sitemap",
                    Pages = pages,
                    TopIssues = topIssues
                }
            };
        }

        // ───────────────────────────────────────────────────────── helper factories
        static AccessibilityIssue Issue(
            string code, string title, string msg, string sev, string target,
            string ctxHtml, string fix, string cat) => new()
            {
                Code = code,
                Title = title,
                Message = msg,
                Severity = sev,
                Target = target,
                ContextHtml = ctxHtml,
                Fix = fix,
                Category = cat,
                InstanceCount = 1
            };

        static AccessibilityPageResult Page(string url, List<AccessibilityIssue> issues) => new()
        {
            Url = url,
            Issues = issues
        };
    }
}
