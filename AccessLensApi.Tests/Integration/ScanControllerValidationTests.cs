using AccessLensApi.Data;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens.Experimental;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace AccessLensApi.Tests.Integration
{
    public class ScanControllerValidationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private const string CaptchaToken = "XXXX.DUMMY.TOKEN.XXXX";
        private readonly HttpClient _client;
        public required Mock<IHttpClientFactory> _fakeHttpHandler = new Mock<IHttpClientFactory>();
        private readonly ITestOutputHelper _output;

        public ScanControllerValidationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            ScanHelper.SetupMockCaptcha(_fakeHttpHandler, true);
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(_fakeHttpHandler.Object);
                    var connection = new SqliteConnection("DataSource=:memory:");
                    connection.Open();

                    services.AddDbContext<ApplicationDbContext>(o =>
                        o.UseSqlite(connection));
                });
            }).CreateClient();
            _output = output;
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
            var request = new ScanRequest { Url = url, Email = email, CaptchaToken = CaptchaToken };
            var content = ScanHelper.ScanRequestAsFormData(request);

            // Act
            var response = await _client.PostAsJsonAsync("/api/scan/starter", content);

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
            var request = new ScanRequest { Url = url, Email = email, CaptchaToken = CaptchaToken };
            var content = ScanHelper.ScanRequestAsFormData(request);

            // Act
            var response = await _client.PostAsync("/api/scan/starter", content);

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
            var request = new ScanRequest { Url = url, Email = "test@example.com", CaptchaToken = CaptchaToken };
            var content = ScanHelper.ScanRequestAsFormData(request);

            // Act
            var response = await _client.PostAsync("/api/scan/starter", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("URL not allowed", responseContent);
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
            var request = new ScanRequest { Url = url, Email = "test@example.com", CaptchaToken = CaptchaToken };
            var content = ScanHelper.ScanRequestAsFormData(request);

            // Act
            var response = await _client.PostAsync("/api/scan/starter", content);

            // Assert
            // Should not fail due to URL validation (may fail for other reasons like missing services)
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Starter_UrlTooLong_ReturnsBadRequest()
        {
            // Arrange - Create a request that exceeds the 1MB limit
            var largeString = new string('x', 550); // 1.5MB
            var request = new ScanRequest
            {
                Url = "https://example.com" + largeString,
                Email = "test@example.com",
                CaptchaToken = CaptchaToken
            };

            var content = ScanHelper.ScanRequestAsFormData(request);

            // Act
            var response = await _client.PostAsync("/api/scan/starter", content);
            var body = await response.Content.ReadAsStringAsync();

            _output.WriteLine("Status Code: " + response.StatusCode);
            _output.WriteLine("Response Body: " + body);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}