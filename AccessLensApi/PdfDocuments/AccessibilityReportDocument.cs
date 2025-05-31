using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace AccessLensApi.PdfDocuments
{
    public class AccessibilityReportDocument : IDocument
    {
        private readonly string SiteName;
        private readonly List<(string Issue, string Rule, string Severity, int Pages, string ExampleUrl)> Summary;
        private readonly int RulesPassed;
        private readonly int RulesFailed;
        private readonly int TotalRulesTested;
        private readonly int PageCount;

        private const string CompanyName = "Your Company Name";
        private const string CompanyWebsite = "www.yourcompany.com";
        private const string ContactEmail = "contact@yourcompany.com";
        private const string LogoPath = "logo.png"; // Place your logo in the output directory or comment out if not used

        public AccessibilityReportDocument(
            string siteName,
            List<(string Issue, string Rule, string Severity, int Pages, string ExampleUrl)> summary,
            int rulesPassed,
            int rulesFailed,
            int totalRulesTested,
            int pageCount)
        {
            SiteName = siteName;
            Summary = summary;
            RulesPassed = rulesPassed;
            RulesFailed = rulesFailed;
            TotalRulesTested = totalRulesTested;
            PageCount = pageCount;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeCoverContent);
                    page.Footer().Element(ComposeFooter);
                })
                .Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(container =>
                    {
                        container.PaddingBottom(10)
                        .Text("Accessibility Findings")
                        .FontSize(14)
                        .Bold();
                    });

                    page.Content().Element(ComposeFindingsTable);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                // Logo (optional)
                if (System.IO.File.Exists(LogoPath))
                    col.Item()
                    .Height(50)
                    .AlignCenter()
                    .PaddingBottom(10)
                    .Image(LogoPath);



                col.Item()
                   .Text("AccessGuard – Preliminary Accessibility Scan")
                   .FontSize(20)
                   .Bold()
                   .AlignCenter();

                col.Item()
                   .PaddingBottom(10)
                   .Text(SiteName)
                   .FontSize(14)
                   .SemiBold()
                   .AlignCenter();
            });
        }

        void ComposeCoverContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(10);

                // Date and pages info
                col.Item()
                   .Text($"Date: {DateTime.Today:MMMM d, yyyy}    |    Pages crawled: {PageCount}")
                   .FontSize(10)
                   .AlignCenter();

                // "Summary" header
                col.Item()
                   .PaddingTop(10)
                   .Text("Summary")
                   .FontSize(14)
                   .Bold();

                // Stats table
                col.Item().Column(stats =>
                {
                    stats.Spacing(2);

                    stats.Item().Text(txt =>
                    {
                        txt.Span("• Rules tested: ").SemiBold();
                        txt.Span(TotalRulesTested.ToString());
                    });

                    stats.Item().Text(txt =>
                    {
                        txt.Span("• Rules passed: ").SemiBold();
                        txt.Span(RulesPassed.ToString());
                    });

                    stats.Item().Text(txt =>
                    {
                        txt.Span("• Rules failing: ").SemiBold().FontColor(Colors.Red.Medium);
                        txt.Span(RulesFailed.ToString()).FontColor(Colors.Red.Medium);
                    });

                    stats.Item().Text(txt =>
                    {
                        txt.Span("• Severity breakdown: ").SemiBold();
                        txt.Span(SeverityCounts(Summary));
                    });
                });

                // Advisory text
                col.Item()
                   .PaddingTop(10)
                   .Text("Critical and Serious issues should be resolved first to reduce legal exposure and improve usability for assistive-technology users.")
                   .FontSize(11)
                   .Italic();

                col.Item()
                   .PaddingTop(10)
                   .Text("About WCAG: The Web Content Accessibility Guidelines (WCAG) are the global standard for digital accessibility. This report highlights issues found against the WCAG 2.2 ruleset.")
                   .FontSize(9);

                // Contact info
                col.Item()
                   .PaddingTop(20)
                   .Text($"Contact us: {ContactEmail}")
                   .FontSize(10);
            });
        }

        void ComposeFindingsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Issue
                    columns.RelativeColumn(1); // Rule
                    columns.RelativeColumn(1); // Severity
                    columns.RelativeColumn(1); // Pages
                    columns.RelativeColumn(3); // Example URL
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyleHeader).Text("Issue");
                    header.Cell().Element(CellStyleHeader).Text("Rule");
                    header.Cell().Element(CellStyleHeader).Text("Severity");
                    header.Cell().Element(CellStyleHeader).Text("Pages");
                    header.Cell().Element(CellStyleHeader).Text("Example URL");

                    static IContainer CellStyleHeader(IContainer container)
                    {
                        return container
                            .DefaultTextStyle(x => x.SemiBold())
                            .Background(Colors.Grey.Lighten2)
                            .PaddingVertical(4)
                            .PaddingHorizontal(2);
                    }
                });

                // Rows with alternating background
                int rowIndex = 0;
                foreach (var row in Summary)
                {
                    var background = rowIndex++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Element(c => CellStyleRow(c, background)).Text(row.Issue);
                    table.Cell().Element(c => CellStyleRow(c, background)).Text(row.Rule);
                    table.Cell().Element(c => CellStyleRow(c, background)).Text(row.Severity);
                    table.Cell().Element(c => CellStyleRow(c, background)).Text(row.Pages.ToString());
                    table.Cell().Element(c => CellStyleRow(c, background)).Text(row.ExampleUrl);

                    static IContainer CellStyleRow(IContainer container, string background)
                    {
                        return container
                            .Background(background)
                            .PaddingVertical(2)
                            .PaddingHorizontal(2);
                    }
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem()
                   .AlignLeft()
                   .Text($"{CompanyName} | {CompanyWebsite}")
                   .FontSize(9);

                row.RelativeItem()
                   .AlignRight()
                   .Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                        x.DefaultTextStyle(style => style.FontSize(9));
                    });
            });
        }

        private string SeverityCounts(List<(string Issue, string Rule, string Severity, int Pages, string ExampleUrl)> summary)
        {
            var order = new[] { "Critical", "Serious", "Major", "Moderate", "Minor", "Info" };
            var counts = summary.GroupBy(x => x.Severity).ToDictionary(g => g.Key, g => g.Count());
            return string.Join("  •  ", order.Where(o => counts.ContainsKey(o)).Select(o => $"{o}: {counts[o]}"));
        }
    }
}