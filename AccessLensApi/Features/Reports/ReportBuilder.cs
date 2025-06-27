using AccessLensApi.Models.Unified;
using AccessLensApi.Storage;
using HandlebarsDotNet;
using Microsoft.Playwright;
using System.Collections;

namespace AccessLensApi.Features.Reports
{
    public class ReportBuilder : IReportBuilder
    {
        private readonly string _template;
        private const string TemplatePath = "Features/Reports/Templates/report.html";
        private readonly IStorageService _storage;

        public ReportBuilder(IStorageService storage)
        {
            _template = File.ReadAllText(TemplatePath);
            _storage = storage;
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
            handlebars.RegisterHelper("eq", (writer, context, parameters) =>
            {
                // Expect exactly two params: {{eq a b}}
                var isEqual = parameters.Length >= 2 && Equals(parameters[0], parameters[1]);

                // Handlebars treats any non-empty output as “truthy” inside an #if,
                // so write “true” for match, nothing for mismatch.
                if (isEqual) writer.WriteSafeString("true");
            });

            handlebars.RegisterHelper("length", (writer, context, parameters) =>
            {
                if (parameters.Length > 0)
                {
                    var param = parameters[0];
                    if (param is IEnumerable<object> enumerable)
                    {
                        writer.WriteSafeString(enumerable.Count().ToString());
                    }
                    else if (param is System.Collections.IEnumerable nonGenericEnumerable)
                    {
                        var count = 0;
                        foreach (var item in nonGenericEnumerable)
                        {
                            count++;
                        }
                        writer.WriteSafeString(count.ToString());
                    }
                    else
                    {
                        writer.WriteSafeString("0");
                    }
                }
                else
                {
                    writer.WriteSafeString("0");
                }
            });

            var compiled = handlebars.Compile(_template);
            return compiled(model);
        }

        public async Task<string> GeneratePdfAsync(string html)
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            Directory.CreateDirectory("reports");

            var pdf = await page.PdfAsync(new PagePdfOptions
            {
                Path = "reports/accessibility-report.pdf",
                Format = "A4",
                DisplayHeaderFooter = true,
                FooterTemplate = "<div style='font-size:10px;width:100%;text-align:right;margin-right:10px;'>Page <span class='pageNumber'></span> of <span class='totalPages'></span></div>",
                Margin = new Margin { Bottom = "40px" },
                PrintBackground = true
            });

            string key = $"reports/{Guid.NewGuid()}.pdf";

            await _storage.UploadAsync(key, pdf);

            // 30-day presigned URL
            return _storage.GetPresignedUrl(key, TimeSpan.FromDays(6.95));
        }

        private static string GeneratePageChartUrl(int critical, int serious, int moderate, int minor)
        {
            // Build dynamic pie slices – severities with a value of 0 are removed
            var rawValues = new[] { critical, serious, moderate, minor };
            var rawLabels = new[] { "Critical", "Serious", "Moderate", "Minor" };

            // WCAG-AA, print-friendly palette (override later if you need)
            var colorPalette = new[] { "#B3261E", "#E88E00", "#D0A000", "#0B7C5C" };

            var slices = rawValues
                .Select((value, index) => new { value, label = rawLabels[index], color = colorPalette[index] })
                .Where(s => s.value > 0)
                .ToArray();

            var chart = new
            {
                type = "pie",
                data = new
                {
                    labels = slices.Select(s => s.label),
                    datasets = new[]
                    {
                new
                {
                    data            = slices.Select(s => s.value),
                    backgroundColor = slices.Select(s => s.color),
                    borderWidth     = 2,
                    borderColor     = "white"
                }
            }
                },
                options = new
                {
                    plugins = new
                    {
                        legend = new { position = "bottom" },
                        datalabels = new
                        {
                            formatter = "function(v){return v + '%';}",
                            color = "#000",
                            font = new { weight = "bold" }
                        }
                    },
                    backgroundColor = "white"
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(chart);
            return $"https://quickchart.io/chart?c={Uri.EscapeDataString(json)}&plugins=datalabels";
        }
    }
}
