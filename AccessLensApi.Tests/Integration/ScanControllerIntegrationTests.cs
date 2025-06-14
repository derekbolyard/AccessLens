using AccessLensApi.Data;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Middleware;
using AccessLensApi.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace AccessLensApi.Tests.Integration
{
    /// <summary>
    /// Integration tests that use real services where possible,
    /// only mocking external dependencies that are expensive or unreliable
    /// </summary>
    public class ScanControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;

        public ScanControllerIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _output = output;

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
                    _mockHttpClientFactory = new Mock<IHttpClientFactory>();
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton(_mockHttpClientFactory.Object);

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

                    services.Configure<CaptchaOptions>(opt =>
                    {
                        opt.hCaptchaSecret = "test-secret-key";
                    });
                });

                builder.UseEnvironment("Testing");
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
                Email = "realtest@example.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            Assert.True(result.TryGetProperty("score", out var score));
            Assert.True(result.TryGetProperty("pdfUrl", out var pdfUrl));
            Assert.True(result.TryGetProperty("teaserUrl", out var teaserUrl));

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
            // Arrange
            var request = new ScanRequest
            {
                Url = "https://example.com",
                Email = "ratelimit@example.com"
            };

            // Act - Make multiple rapid requests
            var tasks = Enumerable.Range(0, 15) // More than the limit of 10
                .Select(_ => _client.PostAsJsonAsync("/api/scan/starter", request))
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert - Some should be rate limited
            var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            Assert.True(rateLimitedCount > 0, "Expected some requests to be rate limited");
        }

        [Fact]
        public async Task FullSiteScan_RealCreditManager_ChecksPremiumAccess()
        {
            // Arrange
            this.SetupMockHttp(captchaSuccess: true); // Simulate successful captcha validation
            var request = new FullScanRequest
            {
                Url = "https://example.com",
                Email = "basic@example.com",
                HcaptchaToken = "test-token" // TestHttpClientFactory will handle this
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

        private void SetupMockHttp(bool captchaSuccess)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                   .Setup<Task<HttpResponseMessage>>(
                       "SendAsync",
                       ItExpr.IsAny<HttpRequestMessage>(),
                       ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(new HttpResponseMessage
                   {
                       StatusCode = HttpStatusCode.OK,
                       Content = new StringContent(
                           $"{{\"success\":{captchaSuccess.ToString().ToLowerInvariant()}}}")
                   });

            var httpClient = new HttpClient(handler.Object);
            
            _mockHttpClientFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
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