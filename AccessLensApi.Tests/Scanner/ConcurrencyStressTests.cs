using AccessLensApi.Features.Scans.Models;
using RichardSzalay.MockHttp;
using System.Net;
using System.Threading;

namespace AccessLensApi.Tests.Scanner
{
    [Collection("NoParallel")]          // ensure other tests don’t run with it
    public class ConcurrencyStressTests
    {
        private const int Pages = 10;   // root + nine children
        private const int LatencyMs = 200;  // artificial server latency
        private const int MaxConc = 2;    // MaxConcurrency we’re validating

        [Fact(DisplayName = "Scanner never exceeds MaxConcurrency")]
        [Trait("Category", "Integration")]
        public async Task Scanner_respects_MaxConcurrency()
        {
            // ── concurrency counters ────────────────────────────────────────
            int current = 0;   // requests in flight *right now*
            int maxSeen = 0;   // largest value current ever reached

            HttpResponseMessage DelayedHtml(string body)
            {
                // increment + update maxSeen atomically
                int now = Interlocked.Increment(ref current);
                InterlockedExtensions.UpdateMax(ref maxSeen, now);

                Thread.Sleep(LatencyMs);    // simulate network latency
                Interlocked.Decrement(ref current);

                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(body) };
            }

            // ── mock HTTP server ────────────────────────────────────────────
            var mockHttp = new MockHttpMessageHandler();

            // root contains links to page1 … page9
            var links = string.Join("",
                Enumerable.Range(1, Pages - 1)
                          .Select(i => $"<a href=\"https://slow.com/page{i}\">p{i}</a>"));

            mockHttp.When("https://slow.com/page0")
                    .Respond(_ => DelayedHtml($"<html>{links}</html>"));

            for (int i = 1; i < Pages; i++)
            {
                mockHttp.When($"https://slow.com/page{i}")
                        .Respond(_ => DelayedHtml("<html></html>"));
            }

            mockHttp.When("*cdnjs.cloudflare.com*")
                    .Respond("application/javascript", "/* axe-core */");

            var scanner = ScannerFactory.CreateScanner(mockHttp.ToHttpClient());

            // ── execute scan ────────────────────────────────────────────────
            await scanner.ScanAllPagesAsync("https://slow.com/page0", new ScanOptions
            {
                MaxPages = Pages,
                MaxConcurrency = MaxConc,
                MaxDepth = 1           // root → leaf links only
            });

            // ── assert true concurrency ────────────────────────────────────
            Assert.True(maxSeen <= MaxConc,
                $"Expected ≤{MaxConc} simultaneous requests, observed {maxSeen}.");
        }
    }

    // simple helper to atomically track a running maximum
    internal static class InterlockedExtensions
    {
        public static void UpdateMax(ref int target, int candidate)
        {
            int snapshot;
            while (candidate > (snapshot = Volatile.Read(ref target)) &&
                   Interlocked.CompareExchange(ref target, candidate, snapshot) != snapshot) { }
        }
    }

    // collection marker to disable xUnit parallelisation
    [CollectionDefinition("NoParallel", DisableParallelization = true)]
    public sealed class NoParallelCollection { }
}
