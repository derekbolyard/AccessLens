using AccessLensApi.Features.Scans.Models;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessLensApi.Tests.Scanner
{
    public class ConcurrencyStressTests
    {
        [Fact(DisplayName = "MaxConcurrency caps parallel scans")]
        [Trait("Category", "Integration")]
        public async Task MaxConcurrency_limits_parallelism()
        {
            const int Pages = 10;   // number of synthetic pages
            const int LatencyMs = 200;  // simulated server latency per page

            // ── HTTP stub: each page waits 200 ms then returns minimal HTML ─────
            var mockHttp = new MockHttpMessageHandler();
            for (int i = 0; i < Pages; i++)
            {
                mockHttp.When($"https://slow.com/page{i}")
                        .Respond(async _ =>
                        {
                            await Task.Delay(LatencyMs);
                            return new System.Net.Http.HttpResponseMessage(
                                       System.Net.HttpStatusCode.OK)
                            { Content = new StringContent("<html></html>") };
                        });
            }
            mockHttp.When("*cdnjs.cloudflare.com*")
                    .Respond("application/javascript", "/* axe-core */");

            var scanner = ScannerFactory.CreateScanner(mockHttp.ToHttpClient());

            // ── Run scan & time it ─────────────────────────────────────────────
            var sw = Stopwatch.StartNew();
            await scanner.ScanAllPagesAsync("https://slow.com/page0",
                new ScanOptions
                {
                    MaxPages = Pages,
                    MaxConcurrency = 2,        // cap at 2 parallel tasks
                    MaxDepth = 0         // prevents crawler from adding links
                });
            sw.Stop();

            // Sequential runtime ≈ 10 × 200 ms = 2 s. With 2 threads we expect < 2 s.
            Assert.True(sw.ElapsedMilliseconds < 2000,
                $"Expected <2 s, got {sw.ElapsedMilliseconds} ms.");
        }
    }
}
