using AccessLensApi.Data;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Middleware;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Services.Scanning;
using AccessLensApi.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AccessLensApi.Tests.Integration
{
    /// <summary>
    /// Integration tests that use real services where possible,
    /// only mocking external dependencies that are expensive or unreliable
    /// </summary>
    public class ScanControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private const string CaptchaToken = "XXXX.DUMMY.TOKEN.XXXX";
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ScanControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.Single(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    services.Remove(descriptor);

                    var connection = new SqliteConnection("DataSource=:memory:");
                    connection.Open();

                    services.AddDbContext<ApplicationDbContext>(o =>
                        o.UseSqlite(connection));

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Replace only truly external services with test doubles
                    services.RemoveAll<IA11yScanner>();
                    services.AddSingleton<IA11yScanner, TestA11yScanner>();

                    services.RemoveAll<IPdfService>();
                    services.AddSingleton<IPdfService, TestPdfService>();

                    var httpFactory = new Mock<IHttpClientFactory>();
                    ScanHelper.SetupMockCaptcha(httpFactory, true);
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton(httpFactory.Object);

                    // Keep real implementations of internal services
                    // - CreditManager: Tests real business logic
                    // - RateLimiter: Tests real rate limiting logic
                    // - HttpClientFactory: Use real HTTP client with test endpoints

                    // Override configuration for testing
                    services.Configure<RateLimitingOptions>(opt =>
                    {
                        opt.MaxScanDurationSeconds = 30; // Shorter timeouts for tests
                        opt.MaxStarterScansPerHour = 2;
                    });
                    services.AddAuthentication(defaultScheme: "TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                     "TestScheme", _ => { });
                });

                builder.UseEnvironment("Development");
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Starter_ValidRequest_UsesRealServicesAndDatabase()
        {
            // Arrange
            var request = new ScanRequest
            {
                Url = "https://example.com",
                Email = "realtest@example.com",
                CaptchaToken = CaptchaToken
            };

            var content = ScanHelper.ScanRequestAsFormData(request);

            // Act
            var response = await _client.PostAsync("/api/scan/starter", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.True(result.TryGetProperty("score", out var score));
            Assert.True(result.TryGetProperty("pdfUrl", out var pdfUrl));
            Assert.True(result.TryGetProperty("teaser", out var teaser));

            // Verify real database interactions
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Users.FindAsync("realtest@example.com");
            Assert.NotNull(user);
            Assert.False(user.FirstScan); // Should be updated by real service

            var report = await dbContext.Reports
                .FirstOrDefaultAsync(r => r.Email == "realtest@example.com");
            Assert.NotNull(report);
            Assert.Equal("Completed", report.Status);
        }

        [Fact]
        public async Task Starter_RealRateLimiting_EnforcesLimits()
        {
            // Arrange – template object is fine
            var template = new ScanRequest
            {
                Url = "https://example.com",
                Email = "ratelimit@example.com",
                CaptchaToken = CaptchaToken
            };

            var i = 0;

            // Act – spin up 15 separate requests, each with its own body
            var tasks = Enumerable.Range(0, 15)
                .Select(_ =>
                {
                    // new content instance each time
                    template.Email = template.Email + i.ToString();
                    i++;
                    var content = ScanHelper.ScanRequestAsFormData(template);
                    return _client.PostAsync("/api/scan/starter", content);
                })
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert – at least one request should have hit the rate-limit
            var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            var messages = responses.Select(r => r.Content.ReadAsStringAsync());
            Assert.True(rateLimitedCount > 0, "Expected some requests to be rate limited");
            Assert.True(responses.All(r => r.StatusCode != HttpStatusCode.BadRequest));
            Assert.True(responses.All(r => r.StatusCode != HttpStatusCode.InternalServerError));
        }

        [Fact]
        public async Task FullSiteScan_RealCreditManager_ChecksPremiumAccess()
        {
            // Arrange
            var request = new FullScanRequest
            {
                Url = "https://example.com",
                Email = "basic@example.com",
                CaptchaToken = CaptchaToken
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/full", request);

            // Assert - Real credit manager should deny access for non-premium user
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("premium access", content, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    /// <summary>
    /// Test double for A11yScanner that simulates real scanner behavior
    /// without making actual web requests
    /// </summary>
    public class TestA11yScanner : IA11yScanner
    {
        public Task<System.Text.Json.Nodes.JsonObject> ScanFivePagesAsync(string url, CancellationToken cancellationToken = default)
        {
            // Simulate scanning delay
            return Task.Delay(100, cancellationToken).ContinueWith(_ =>
            {
                var result = System.Text.Json.Nodes.JsonNode.Parse($$"""
                {
                    "pages": [
                        {
                            "pageUrl": "{{url}}",
                            "issues": [
                                {
                                    "code": "color-contrast.AA",
                                    "type": "error",
                                    "message": "Element has insufficient color contrast of 2.93 (foreground color: #767676, background color: #ffffff, font size: 9.0pt, font weight: normal). Expected contrast ratio of 4.5:1"
                                },
                                {
                                    "code": "aria-hidden-focus",
                                    "type": "error", 
                                    "message": "Focusable element should not have aria-hidden=true"
                                }
                            ]
                        }
                    ],
                    "teaserUrl": "{{url}}/teaser.png",
                    "scannedAt": "{{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}}"
                }
                """) as System.Text.Json.Nodes.JsonObject;

                return result ?? throw new InvalidOperationException("Failed to create test scan result");
            }, cancellationToken);
        }

        public Task<System.Text.Json.Nodes.JsonObject> ScanAllPagesAsync(string url, ScanOptions options, CancellationToken cancellationToken = default)
        {
            // Simulate longer scanning delay for full scans
            return Task.Delay(200, cancellationToken).ContinueWith(_ =>
            {
                var pageCount = Math.Min(options.MaxPages == 0 ? 5 : options.MaxPages, 5);
                var pages = Enumerable.Range(0, pageCount).Select(i => $$"""
                {
                    "pageUrl": "{{url}}{{(i == 0 ? "" : $"/page{i}")}}",
                    "issues": [
                        {
                            "code": "color-contrast.AA",
                            "type": "error",
                            "message": "Color contrast issue on page {{i + 1}}"
                        }
                    ]
                }
                """).ToArray();

                var result = System.Text.Json.Nodes.JsonNode.Parse($$"""
                {
                    "pages": [{{string.Join(",", pages)}}],
                    "totalPages": {{pageCount}},
                    "teaserUrl": "{{url}}/teaser.png",
                    "scannedAt": "{{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}}"
                }
                """) as System.Text.Json.Nodes.JsonObject;

                return result ?? throw new InvalidOperationException("Failed to create test full scan result");
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Test double for PDF service that simulates PDF generation
    /// without actually creating files or uploading to cloud storage
    /// </summary>
    public class TestPdfService : IPdfService
    {
        public Task<string> GenerateAndUploadPdf(string url, JsonNode json)
        {
            // Simulate PDF generation delay
            return Task.Delay(50).ContinueWith(_ =>
            {
                var fileName = $"report-{Guid.NewGuid()}.pdf";
                return $"https://test-storage.example.com/{fileName}";
            });
        }
    }
}