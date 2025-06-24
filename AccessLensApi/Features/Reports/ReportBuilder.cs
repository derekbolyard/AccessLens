using HandlebarsDotNet;
using Microsoft.Playwright;

namespace AccessLensApi.Features.Reports
{
    public class ReportBuilder
    {
        private readonly string _template;

        public ReportBuilder(string templatePath)
        {
            _template = File.ReadAllText(templatePath);
        }

        public string RenderHtml(AccessibilityReport model)
        {
            foreach (var page in model.Pages)
            {
                var chartUrl = GeneratePageChartUrl(
                    page.CriticalCount,
                    page.SeriousCount,
                    page.ModerateCount,
                    page.MinorCount
                );

                page.PageChartUrl = chartUrl;
            }

            var handlebars = Handlebars.Create();
            var compiled = handlebars.Compile(_template);
            return compiled(model);
        }

        public async Task GeneratePdfAsync(string html, string outputPath)
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            Directory.CreateDirectory("reports");
            await page.PdfAsync(new PagePdfOptions
            {
                Path = "reports/accessibility-report.pdf",
                Format = "A4",
                DisplayHeaderFooter = true,
                FooterTemplate = "<div style='font-size:10px;width:100%;text-align:right;margin-right:10px;'>Page <span class='pageNumber'></span> of <span class='totalPages'></span></div>",
                Margin = new Margin { Bottom = "40px" },
                PrintBackground = true
            });
        }

        public static string GeneratePageChartUrl(int critical, int serious, int moderate, int minor)
        {
            var total = critical + serious + moderate + minor;
            var chartData = new
            {
                type = "pie",
                data = new
                {
                    labels = new[] { "Critical", "Serious", "Moderate", "Minor" },
                    datasets = new[]
          {
            new
            {
                data = new[] { critical, serious, moderate, minor },
                backgroundColor = new[] { "#b91c1c", "#ea580c", "#d97706", "#16a34a" }
            }
        }
                },
                options = new
                {
                    plugins = new
                    {
                        datalabels = new
                        {
                            formatter = "function(value, ctx) {\n  let sum = 0;\n  let dataArr = ctx.chart.data.datasets[0].data;\n  dataArr.map(data => sum += data);\n  let percentage = (value * 100 / sum).toFixed(1) + '%';\n  return percentage;\n}",
                            color = "#000",
                            font = new { weight = "bold", size = 14 }
                        },
                        legend = new { position = "bottom", labels = new { color = "#000" } }
                    },
                    backgroundColor = "white"
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(chartData);
            return $"https://quickchart.io/chart?c={Uri.EscapeDataString(json)}&plugins=datalabels";
        }


    }
}
