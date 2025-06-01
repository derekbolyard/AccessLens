using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using AccessLensApi.Utilities;
using Microsoft.Playwright;
using System.Text.Json.Nodes;

namespace AccessLensApi.Services
{
    public sealed class A11yScanner : IA11yScanner, IAsyncDisposable
    {
        private readonly IBrowser _browser;
        private readonly IStorageService _storage;
        private readonly ILogger<A11yScanner> _log;
        private const string AxeCdn = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.0/axe.min.js";

        public A11yScanner(
            IBrowser browser,
            IStorageService storage,
            ILogger<A11yScanner> log)
        {
            _browser = browser;
            _storage = storage;
            _log = log;
        }

        public async Task<JsonObject> ScanFivePagesAsync(string rootUrl)
        {
            var ctx = await _browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pagesArr = new JsonArray();
            string? teaserUrl = null;

            queue.Enqueue(rootUrl);

            while (queue.Count > 0 && pagesArr.Count < 5)
            {
                var url = queue.Dequeue();
                if (!visited.Add(url)) continue;

                var page = await ctx.NewPageAsync();
                await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

                // ── 1️⃣  run axe ───────────────────────────────────────────────
                await page.AddScriptTagAsync(new() { Url = AxeCdn });
                string axeJson = await page.EvaluateAsync<string>(
                    @"async () => JSON.stringify(await axe.run({ resultTypes:['violations'] }))",
                    null);

                // ── 2️⃣  teaser on FIRST page ─────────────────────────────────
                if (pagesArr.Count == 0)
                {
                    var axeObj = (JsonObject)JsonNode.Parse(axeJson)!;
                    var violations = axeObj["violations"]!.AsArray();

                    int crit = violations.Count(v => v?["impact"]?.ToString() == "critical");
                    int seri = violations.Count(v => v?["impact"]?.ToString() == "serious");
                    int moderate = violations.Count(v => v?["impact"]?.ToString() == "moderate");
                    int score = A11yScore.From(axeObj);

                    /* ---- capture (crop if possible) ---- */
                    (byte[] raw, bool zoomed) =
                        await ScreenshotHelper.CaptureAsync(page, violations);

                    /* ---- overlay bar + circles ---- */
                    byte[] finalTeaser = TeaserOverlay.AddOverlay(raw, score, crit, seri, moderate);

                    /* ---- upload & URL ---- */
                    string key = $"teasers/{Guid.NewGuid()}.png";
                    await _storage.UploadAsync(key, finalTeaser);
                    teaserUrl = _storage.GetPresignedUrl(key, TimeSpan.FromDays(30));

                    _log.LogInformation("Teaser built (zoomed={Zoom}, crit={Crit}, ser={Ser})", zoomed, crit, seri);
                }

                // ── 3️⃣  add Shape-A JSON for this page ───────────────────────
                pagesArr.Add(ConvertToShapeA(url, axeJson));

                // ── 4️⃣  enqueue internal links ──────────────────────────────
                var hrefs = await page.EvaluateAsync<string[]>(
                    "Array.from(document.querySelectorAll('a[href]'))" +
                    ".map(a => new URL(a.href, document.baseURI).href)",
                    null);

                foreach (var href in hrefs.Take(30))
                    if (href.StartsWith(rootUrl, StringComparison.OrdinalIgnoreCase) &&
                        !visited.Contains(href))
                        queue.Enqueue(href);

                await page.CloseAsync();
            }

            await ctx.CloseAsync();

            return new JsonObject
            {
                ["pages"] = pagesArr,
                ["teaserUrl"] = teaserUrl ?? string.Empty
            };
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
