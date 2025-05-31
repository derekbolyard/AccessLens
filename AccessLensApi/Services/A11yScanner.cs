using Microsoft.Playwright;
using System.Text.Json.Nodes;

namespace AccessLensApi.Services
{
    public sealed class A11yScanner : IA11yScanner, IAsyncDisposable
    {
        private readonly IBrowser _browser;
        private const string AxeCdn = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.0/axe.min.js";

        private A11yScanner(IBrowser browser) => _browser = browser;

        public static async Task<A11yScanner> CreateAsync()
        {
            var pw = await Playwright.CreateAsync();
            var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
            return new A11yScanner(browser);
        }

        public async Task<JsonArray> ScanFivePagesAsync(string rootUrl)
        {
            var ctx = await _browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allPages = new JsonArray();                        // ← what PdfService needs

            queue.Enqueue(rootUrl);

            while (queue.Count > 0 && allPages.Count < 5)
            {
                string url = queue.Dequeue();
                if (!visited.Add(url)) continue;                    // skip duplicates

                /* ---------- open page & run axe ---------- */
                var page = await ctx.NewPageAsync();
                await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

                await page.AddScriptTagAsync(new() { Url = AxeCdn });
                string axeJson = await page.EvaluateAsync<string>(
                    @"async () => JSON.stringify(
                        await axe.run({ resultTypes:['violations'] })
                      )");


                /* ---------- transform to “Shape A” ---------- */
                allPages.Add(ConvertToShapeA(url, axeJson));

                /* ---------- harvest internal links ---------- */
                var hrefs = await page.EvaluateAsync<string[]>(
                    "Array.from(document.querySelectorAll('a[href]'))" +
                    ".map(a => new URL(a.href, document.baseURI).href)");

                foreach (var href in hrefs.Take(30))                // cap fan-out
                    if (href.StartsWith(rootUrl, StringComparison.OrdinalIgnoreCase) &&
                        !visited.Contains(href))
                        queue.Enqueue(href);

                await page.CloseAsync();
            }

            await ctx.CloseAsync();
            return allPages;
        }
        private static JsonObject ConvertToShapeA(string pageUrl, string axeJson)
        {
            var axe = (JsonObject)JsonNode.Parse(axeJson)!;
            var issuesArr = new JsonArray();

            foreach (var v in axe["violations"]!.AsArray())
            {
                issuesArr.Add(new JsonObject
                {
                    ["type"] = v!["impact"]!.GetValue<string>(),        // critical, serious…
                    ["code"] = v!["id"]!.GetValue<string>(),            // e.g. color-contrast
                    ["message"] = v!["help"]!.GetValue<string>(),
                    ["context"] = v!["nodes"]![0]!["html"]!.GetValue<string>()
                });
            }

            return new JsonObject
            {
                ["pageUrl"] = pageUrl,
                ["issues"] = issuesArr
            };
        }

        public async ValueTask DisposeAsync() => await _browser.DisposeAsync();
    }
}
