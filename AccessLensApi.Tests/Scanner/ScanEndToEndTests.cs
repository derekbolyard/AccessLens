using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Services;
using AccessLensApi.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using RichardSzalay.MockHttp;
using System.Net;

namespace AccessLensApi.Tests.Scanner
{
    public sealed class ScanEndToEndTests : IAsyncLifetime
    {
        private IPlaywright _pw = default!;
        private IBrowser _browser = default!;

        public async Task InitializeAsync()
        {
            _pw = await Playwright.CreateAsync();
            _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
        }

        public async Task DisposeAsync()
        {
            await _browser.DisposeAsync();
            _pw.Dispose();
        }

        // ────────────────────────────────────────────────────────────────────
        // 1️⃣  Sitemap honoured
        // ────────────────────────────────────────────────────────────────────
        [Fact(DisplayName = "Crawler honors sitemap when UseSitemap=true")]
        [Trait("Category", "Integration")]
        public async Task Crawler_reads_sitemap()
        {
            // ----- Arrange --------------------------------------------------
            int port = PortHelper.GetFreePort();
            using var host = await TestSiteBuilder.RunAsync(port, app =>
            {
                app.MapGet("/", () => Results.Text("<h1>Root</h1>", "text/html"));
                app.MapGet("/index.html", () => Results.Text("<h1>Home</h1>", "text/html"));
                app.MapGet("/pricing.html", () => Results.Text("<h1>Pricing</h1>", "text/html"));

                app.MapGet("/robots.txt", ctx =>
                    ctx.Response.WriteAsync($"Sitemap: http://localhost:{port}/sitemap.xml"));

                app.MapGet("/sitemap.xml", (HttpContext ctx) =>
                {
                    var port = ctx.Request.Host.Port ?? 80;
                    var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
                          <url><loc>http://localhost:{port}/index.html</loc></url>
                          <url><loc>http://localhost:{port}/pricing.html</loc></url>
                        </urlset>";
                    return Results.Text(xml, "application/xml");
                });
            });

            var scanner = ScannerFactory.BuildScanner(
                              _browser,
                              new HttpClient(),
                              new AxeScriptProvider(new HttpClient(), new NullLogger<AxeScriptProvider>()),
                              new InMemoryStorage());

            // ----- Act ------------------------------------------------------
            var opts = new ScanOptions
            {
                UseSitemap = true,
                MaxPages = 10,
                MaxDepth = 0,   // rely only on sitemap
                MaxLinksPerPage = 20
            };

            var json = await scanner.ScanAllPagesAsync($"http://localhost:{port}/", opts);
            var pages = json["pages"]!.AsArray()
                           .Select(n => n!["pageUrl"]!.GetValue<string>())
                           .Order()
                           .ToArray();

            // ----- Assert ---------------------------------------------------
            Assert.Equal(new[]
            {
            $"http://localhost:{port}/",
            $"http://localhost:{port}/index.html",
            $"http://localhost:{port}/pricing.html"
        }, pages);

            Assert.Equal("sitemap+crawling", json["discoveryMethod"]!.GetValue<string>());
        }

        // ────────────────────────────────────────────────────────────────────
        // 2️⃣  Depth-limited crawl
        // ────────────────────────────────────────────────────────────────────
        [Fact(DisplayName = "Crawler traverses same-site links up to MaxDepth")]
        [Trait("Category", "Integration")]
        public async Task Crawler_discovers_expected_internal_pages()
        {
            // root  → /about.html              (depth 1)
            //       → https://evil.com/        (ignored – off-site)
            // about → /contact.html            (depth 2 > MaxDepth ⇒ skipped)

            int port = PortHelper.GetFreePort();
            using var host = await TestSiteBuilder.RunAsync(port, app =>
            {
                app.MapGet("/root.html", () => Results.Text(
                    @$"<a href=""/about.html"">About</a>
                  <a href=""https://evil.com/"">Hacker</a>", "text/html"));

                app.MapGet("/about.html", () => Results.Text(
                    @"<a href=""/contact.html"">Contact</a>", "text/html"));

                app.MapGet("/contact.html", () => Results.Text("<h1>Contact</h1>", "text/html"));
            });

            var scanner = ScannerFactory.BuildScanner(
                              _browser,
                              new HttpClient(),
                              new AxeScriptProvider(new HttpClient(), new NullLogger<AxeScriptProvider>()),
                              new InMemoryStorage()
                              );

            var scanOpts = new ScanOptions
            {
                MaxPages = 10,
                MaxDepth = 1,      // root (0) + about (1)
                MaxLinksPerPage = 10
            };

            var json = await scanner.ScanAllPagesAsync($"http://localhost:{port}/root.html", scanOpts);
            var crawled = json["pages"]!.AsArray()
                              .Select(n => n!["pageUrl"]!.GetValue<string>())
                              .Order()
                              .ToArray();

            Assert.Equal(new[]
            {
            $"http://localhost:{port}/about.html",
            $"http://localhost:{port}/root.html"
        }, crawled);
        }
    }

    // ───────────────────────────────────────────────────────────────────────────────
    // Helper: run a minimal Kestrel host with custom endpoints
    // ───────────────────────────────────────────────────────────────────────────────
    static class TestSiteBuilder
    {
        public static async Task<IHost> RunAsync(
            int port,
            Action<IEndpointRouteBuilder> mapEndpoints)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(web =>
                {
                    web.UseKestrel()
                       .UseUrls($"http://localhost:{port}")
                       .Configure(app =>
                       {
                           app.UseRouting();

                           app.UseEndpoints(endpoints =>
                           {
                               mapEndpoints(endpoints);
                           });
                       });
                })
                .Build();

            await host.StartAsync();
            return host;
        }
    }

    static class PortHelper
    {
        public static int GetFreePort()
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }

    // ───────────────────────────────────────────────────────────────────────────────
    // Stupid-simple fake for IStorageService so the scanner compiles
    // ───────────────────────────────────────────────────────────────────────────────
    public sealed class InMemoryStorage : IStorageService
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public Task UploadAsync(string key, byte[] bytes, CancellationToken cancellationToken) { _store[key] = bytes; return Task.CompletedTask; }
        public string GetPresignedUrl(string key, TimeSpan _) => $"memory://{key}";
    }
}
