using AccessLensApi.Features.Reports.Models;       // AccessibilityReport & friends
using AccessLensApi.Models.ScannerDtos;            // A11yScanResult & friends

namespace AccessLensApi.Features.Reports
{
    /// <summary>
    /// Converts raw scan results into a presentation-ready <see cref="AccessibilityReport"/>.
    /// </summary>
    public static class ReportMapper
    {
        /// <summary>
        /// Builds an <see cref="AccessibilityReport"/> from an <see cref="A11yScanResult"/>.
        /// Extra visual / white-label metadata is passed in as parameters so the mapper
        /// stays pure and testable.
        /// </summary>
        public static AccessibilityReport ToAccessibilityReport(
            A11yScanResult scan,
            string siteUrl,
            // ─── white-label / branding fields ───────────────────────────────
            bool whiteLabel = false,
            string clientName = "",
            string clientLogoUrl = "",
            string primaryColor = "#2563eb",
            string secondaryColor = "#16a34a",
            string footerText = "",
            string contactEmail = "",
            string clientWebsite = "",
            string consultationLink = "",
            // ─── copy / narrative fields (pass what you have) ───────────────
            string legalRisk = "",
            string commonViolations = ""
        )
        {
            // ─── derive overall score (simple weighting; tweak as desired) ───
            var allIssues = scan.Pages.SelectMany(p => p.Issues).ToList();
            double weighted =
                allIssues.Count(i => i.Type.Equals("critical", StringComparison.OrdinalIgnoreCase)) * 5 +
                allIssues.Count(i => i.Type.Equals("serious", StringComparison.OrdinalIgnoreCase)) * 3 +
                allIssues.Count(i => i.Type.Equals("moderate", StringComparison.OrdinalIgnoreCase)) * 1;

            // “perfect” is 0 issues → score 100; caps at 0.
            int overallScore = Math.Max(0, 100 - (int)weighted);

            // ─── screenshots (teaser, if present) ────────────────────────────
            var screenshots = new List<ReportImage>();
            if (!string.IsNullOrWhiteSpace(scan.Teaser?.Url))
            {
                screenshots.Add(new ReportImage
                {
                    Src = scan.Teaser!.Url!,
                    Alt = "Accessibility scan teaser"
                });
            }

            // ─── map pages ───────────────────────────────────────────────────
            var pages = scan.Pages
                .Select(p =>
                {
                    var crit = p.Issues.Count(i => i.Type.Equals("critical", StringComparison.OrdinalIgnoreCase));
                    var seri = p.Issues.Count(i => i.Type.Equals("serious", StringComparison.OrdinalIgnoreCase));
                    var mod = p.Issues.Count(i => i.Type.Equals("moderate", StringComparison.OrdinalIgnoreCase));
                    var min = p.Issues.Count(i => i.Type.Equals("minor", StringComparison.OrdinalIgnoreCase));

                    int pageScore = Math.Max(0, 100 - (crit * 5 + seri * 3 + mod));

                    return new Models.PageResult
                    {
                        Url = p.PageUrl,
                        PageScore = pageScore.ToString(),
                        PageChartUrl = string.Empty,      // caller can populate later
                        CriticalCount = crit,
                        SeriousCount = seri,
                        ModerateCount = mod,
                        MinorCount = min,
                        Issues = p.Issues
                            .GroupBy(i => new { i.Code, i.Type })
                            .Select(g => new Models.Issue
                            {
                                Title = g.Key.Code,
                                Description = g.First().Message,
                                Fix = string.Empty,                       // add later if you have it
                                RuleId = g.Key.Code,
                                Target = string.Join(", ", g.Take(3).Select(i => i.ContextHtml)) +
                                                (g.Count() > 3 ? " …" : ""),
                                Severity = g.Key.Type,
                                InstanceCount = g.Count()                           // optional field for CSV/appendix
                            })
                            .ToList()
                    };
                })
                .ToList();

            // ─── build report ────────────────────────────────────────────────
            return new AccessibilityReport
            {
                WhiteLabel = whiteLabel,
                ClientName = clientName,
                ClientLogoUrl = clientLogoUrl,
                SiteUrl = siteUrl,
                ScanDate = scan.ScannedAtUtc.ToString("yyyy-MM-dd"),
                Score = overallScore.ToString(),
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                FooterText = footerText,
                ContactEmail = contactEmail,
                ClientWebsite = clientWebsite,
                ConsultationLink = consultationLink,
                LegalRisk = legalRisk,
                CommonViolations = commonViolations,
                TopIssues = scan.Teaser?.TopIssues?.Select(t =>
                {
                    return new Models.Issue
                    {
                        Title = t.Text,
                        Severity = t.Severity
                    };
                })?.ToList() ?? [],
                Screenshots = screenshots,
                Pages = pages,
                
                // Calculate overall counts across all pages
                CriticalCount = pages.Sum(p => p.CriticalCount),
                SeriousCount = pages.Sum(p => p.SeriousCount),
                ModerateCount = pages.Sum(p => p.ModerateCount),
                MinorCount = pages.Sum(p => p.MinorCount)
            };
        }
    }

}
