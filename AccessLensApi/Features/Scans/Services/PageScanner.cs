using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Features.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Text.Json.Nodes;

namespace AccessLensApi.Features.Scans.Services
{
    internal sealed class PageScanner : IPageScanner
    {
        private readonly IBrowserProvider _provider;
        private readonly IAxeScriptProvider _axe;
        private readonly ILogger<PageScanner> _log;

        public PageScanner(
            IBrowserProvider provider,
            IAxeScriptProvider axe,
            ILogger<PageScanner> log)
        {
            _provider = provider;
            _axe = axe;
            _log = log;
        }

        public async Task<PageScanResult> ScanPageAsync(
            string url,
            bool captureScreenshot,
            CancellationToken ct = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var browser = await _provider.GetBrowserAsync();
            await using var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new() { Width = 1280, Height = 720 }
            });
            await ctx.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,webp,woff2,ttf}", r => r.AbortAsync());

            var axeSrc = await _axe.GetAsync(ct);
            await ctx.AddInitScriptAsync(axeSrc);

            var page = await ctx.NewPageAsync();

            try
            {
                var resp = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 60_000
                });

                stopwatch.Stop();

                if (resp is null)
                {
                    _log.LogWarning("Page {Url} - no response received", url);
                    return new PageScanResult(
                        url, 
                        null, 
                        null, 
                        null, 
                        new ScanFailureInfo(ScanFailureReasons.LoadFailed, "No response received", null, null, stopwatch.Elapsed),
                        stopwatch.Elapsed
                    );
                }

                if (resp.Status >= 400)
                {
                    _log.LogWarning("Page {Url} responded {Status}", url, resp.Status);
                    return new PageScanResult(
                        url, 
                        null, 
                        null, 
                        null, 
                        new ScanFailureInfo(ScanFailureReasons.HttpError, $"HTTP {resp.Status}", resp.Status, null, stopwatch.Elapsed),
                        stopwatch.Elapsed
                    );
                }

                // run axe
                var axeJson = await page.EvaluateAsync<string>(@"
                    new Promise(res =>
                        axe.run(document, (err, results) =>
                            res(JSON.stringify(err ? { violations: [] } : results))
                        )
                    );");

                var axeObj = (JsonObject)JsonNode.Parse(axeJson)!;
                var pageResult = ConvertToPageResult(url, axeObj);

                byte[]? screenshot = null;
                if (captureScreenshot)
                    screenshot = await page.ScreenshotAsync();

                _log.LogInformation("✓ {Url} ({Issues} issues, {Duration}ms)", url, pageResult.Issues.Count, stopwatch.ElapsedMilliseconds);

                return new PageScanResult(url, pageResult, axeObj, screenshot, null, stopwatch.Elapsed);
            }
            catch (TimeoutException ex)
            {
                stopwatch.Stop();
                _log.LogError(ex, "Timeout scanning {Url}", url);
                return new PageScanResult(
                    url, 
                    null, 
                    null, 
                    null, 
                    new ScanFailureInfo(ScanFailureReasons.Timeout, ex.Message, null, null, stopwatch.Elapsed),
                    stopwatch.Elapsed
                );
            }
            catch (PlaywrightException ex)
            {
                stopwatch.Stop();
                _log.LogError(ex, "Browser error scanning {Url}", url);
                return new PageScanResult(
                    url, 
                    null, 
                    null, 
                    null, 
                    new ScanFailureInfo(ScanFailureReasons.BrowserError, ex.Message, null, null, stopwatch.Elapsed),
                    stopwatch.Elapsed
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _log.LogError(ex, "Scan failed for {Url}", url);
                return new PageScanResult(
                    url, 
                    null, 
                    null, 
                    null, 
                    new ScanFailureInfo(ScanFailureReasons.Unknown, ex.Message, null, null, stopwatch.Elapsed),
                    stopwatch.Elapsed
                );
            }
            finally
            {
                await page.CloseAsync();      // explicit cleanup
            }
        }

        /*──────── helpers ────────*/

        private static PageResult ConvertToPageResult(string pageUrl, JsonObject axe)
        {
            var issues = new List<Issue>();

            foreach (var v in axe["violations"]!.AsArray())
            {
                issues.Add(new Issue(
                    Type: v!["impact"]!.GetValue<string>(),
                    Code: v!["id"]!.GetValue<string>(),
                    Message: v!["help"]!.GetValue<string>(),
                    ContextHtml: v!["nodes"]![0]!["html"]!.GetValue<string>()
                ));
            }

            return new PageResult(pageUrl, issues);
        }
    }
}
