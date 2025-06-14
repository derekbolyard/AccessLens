using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace AccessLensApi.Tests.Integration
{
    public class ScanControllerValidationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        public required FakeHttpHandler _fakeHttpHandler;

        public ScanControllerValidationTests(WebApplicationFactory<Program> factory)
        {
            factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    _fakeHttpHandler = new FakeHttpHandler(services);
                });
            });

            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData(null, "test@example.com")]
        [InlineData("", "test@example.com")]
        [InlineData("   ", "test@example.com")]
        [InlineData("not-a-url", "test@example.com")]
        [InlineData("ftp://example.com", "test@example.com")]
        [InlineData("javascript:alert(1)", "test@example.com")]
        public async Task Starter_InvalidUrls_ReturnsBadRequest(string url, string email)
        {
            // Arrange
            var request = new ScanRequest { Url = url, Email = email };

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("https://example.com", null)]
        [InlineData("https://example.com", "")]
        [InlineData("https://example.com", "   ")]
        [InlineData("https://example.com", "not-an-email")]
        [InlineData("https://example.com", "@example.com")]
        [InlineData("https://example.com", "test@")]
        [InlineData("https://example.com", "test.example.com")]
        public async Task Starter_InvalidEmails_ReturnsBadRequest(string url, string email)
        {
            // Arrange
            var request = new ScanRequest { Url = url, Email = email };

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("http://10.0.0.1")]           // Private Class A
        [InlineData("http://172.16.0.1")]        // Private Class B
        [InlineData("http://192.168.1.1")]       // Private Class C
        [InlineData("http://127.0.0.1")]         // Loopback
        [InlineData("http://localhost")]         // Localhost
        [InlineData("http://[::1]")]             // IPv6 loopback
        [InlineData("http://169.254.1.1")]       // Link-local
        public async Task Starter_PrivateNetworkUrls_ReturnsBadRequest(string url)
        {
            // Arrange
            var request = new ScanRequest { Url = url, Email = "test@example.com" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("URL not allowed", content);
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("http://example.com")]
        [InlineData("https://subdomain.example.com")]
        [InlineData("https://example.com:443")]
        [InlineData("http://example.com:80")]
        [InlineData("https://example.com/path")]
        [InlineData("https://example.com/path?query=value")]
        [InlineData("https://example.com/path#fragment")]
        public async Task Starter_ValidPublicUrls_PassesUrlValidation(string url)
        {
            // Arrange
            var request = new ScanRequest { Url = url, Email = "test@example.com" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", request);

            // Assert
            // Should not fail due to URL validation (may fail for other reasons like missing services)
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Starter_RequestTooLarge_ReturnsRequestEntityTooLarge()
        {
            // Arrange - Create a request that exceeds the 1MB limit
            var largeString = new string('x', 1_500_000); // 1.5MB
            var request = new ScanRequest
            {
                Url = "https://example.com" + largeString,
                Email = "test@example.com"
            };

            _fakeHttpHandler.SetupResponse(HttpStatusCode.OK, new HCaptchaVerifyResponse { Success = true});

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", request);

            // Assert
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }

        [Fact]
        public async Task FullSiteScan_RequestTooLarge_ReturnsRequestEntityTooLarge()
        {
            // Arrange - Create a request that exceeds the 2MB limit
            var largeArray = Enumerable.Repeat("x".PadRight(1000, 'x'), 3000).ToArray(); // ~3MB
            var request = new FullScanRequest
            {
                Url = "https://example.com",
                Email = "test@example.com",
                HcaptchaToken = "valid-token",
                Options = new ScanOptions
                {
                    ExcludePatterns = largeArray
                }
            };


            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/full", request);

            // Assert
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }

        [Fact]
        public async Task Starter_MalformedJson_ReturnsBadRequest()
        {
            // Arrange
            var malformedJson = "{\"url\":\"https://example.com\",\"email\":\"test@example.com\""; // Missing closing brace

            // Act
            var response = await _client.PostAsync("/api/scan/starter",
                new StringContent(malformedJson, System.Text.Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FullSiteScan_MalformedJson_ReturnsBadRequest()
        {
            // Arrange
            var malformedJson = "{\"url\":\"https://example.com\",\"email\":\"test@example.com\""; // Missing closing brace

            // Act
            var response = await _client.PostAsync("/api/scan/full",
                new StringContent(malformedJson, System.Text.Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}