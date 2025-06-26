using AccessLensApi.Data;
using AccessLensApi.Features.Scans;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Middleware;
using AccessLensApi.Models;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Services.Scanning;
using AccessLensApi.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json.Nodes;
using Xunit;

namespace AccessLensApi.Tests.Unit
{
    /// <summary>
    /// Unit tests for ScanController focusing on business logic
    /// with all dependencies mocked for isolation
    /// </summary>
    public class ScanControllerUnitTests : IDisposable
    {
        private const string CaptchaToken = "XXXX.DUMMY.TOKEN.XXXX";

        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ICreditManager> _mockCreditManager;
        private readonly Mock<IA11yScanner> _mockScanner;
        private readonly Mock<IPdfService> _mockPdfService;
        private readonly Mock<ILogger<ScanController>> _mockLogger;
        private readonly Mock<IRateLimiter> _mockRateLimiter;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ScanController _controller;

        public ScanControllerUnitTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"UnitTestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockCreditManager = new Mock<ICreditManager>();
            _mockScanner = new Mock<IA11yScanner>();
            _mockPdfService = new Mock<IPdfService>();
            _mockLogger = new Mock<ILogger<ScanController>>();
            _mockRateLimiter = new Mock<IRateLimiter>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockConfiguration = new Mock<IConfiguration>();

            var rateOptions = Options.Create(new RateLimitingOptions
            {
                MaxScanDurationSeconds = 300
            });

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            ScanHelper.SetupMockCaptcha(_mockHttpClientFactory, true);

            _controller = new ScanController(
                _dbContext,
                _mockCreditManager.Object,
                _mockScanner.Object,
                _mockPdfService.Object,
                _mockLogger.Object,
                _mockRateLimiter.Object,
                rateOptions,
                _mockHttpClientFactory.Object,
                envMock.Object,
                _mockConfiguration.Object);

            // Setup HttpContext for IP address
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        public async Task Starter_InvalidUrl_ReturnsBadRequest(string invalidUrl)
        {
            // Arrange
            var request = new ScanRequest { Url = invalidUrl, Email = "test@example.com",
                CaptchaToken = CaptchaToken
            };

            // Act
            var result = await _controller.Starter(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            Assert.NotNull(errorResponse);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        public async Task Starter_InvalidEmail_ReturnsBadRequest(string invalidEmail)
        {
            // Arrange
            var request = new ScanRequest { Url = "https://example.com", Email = invalidEmail,
                CaptchaToken = CaptchaToken
            };

            // Act
            var result = await _controller.Starter(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            Assert.NotNull(errorResponse);
        }

        [Fact]
        public async Task Starter_UnverifiedUserNotFirstScan_ReturnsNeedVerify()
        {
            // Arrange
            this.SetupSuccessfulScanMocks();
            var user = new User
            {
                Email = "unverified@example.com",
                EmailVerified = false,
                FirstScan = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var request = new ScanRequest
            {
                Url = "https://example.com",
                Email = "unverified@example.com",
                CaptchaToken = CaptchaToken
            };

            // Act
            var result = await _controller.Starter(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Use reflection or dynamic to check the anonymous object
            var needVerifyProperty = response.GetType().GetProperty("needVerify");
            Assert.NotNull(needVerifyProperty);
            Assert.True((bool)needVerifyProperty.GetValue(response)!);
        }

        [Fact]
        public async Task Starter_AdminUser_SkipsQuotaCheck()
        {
            // Arrange
            var request = new ScanRequest
            {
                Url = "https://example.com",
                Email = "derekbolyard@gmail.com",
                CaptchaToken = CaptchaToken
            };

            SetupSuccessfulScanMocks();

            // Act
            var result = await _controller.Starter(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Verify quota check was never called
            _mockCreditManager.Verify(x => x.HasQuotaAsync("derekbolyard@gmail.com"), Times.Never);
        }

        [Fact]
        public async Task FullSiteScan_NonPremiumUser_ReturnsBadRequest()
        {
            // Arrange
            var request = new FullScanRequest
            {
                Url = "https://example.com",
                Email = "basic@example.com",
                CaptchaToken = CaptchaToken
            };

            _mockCreditManager
                .Setup(x => x.HasPremiumAccessAsync("basic@example.com"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.FullSiteScan(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            Assert.NotNull(errorResponse);
        }

        private void SetupSuccessfulScanMocks()
        {
            _mockCreditManager
                .Setup(x => x.HasQuotaAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockRateLimiter
                .Setup(x => x.TryAcquireStarterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var mockScanResult = JsonNode.Parse("""
            {
                "pages": [
                    {
                        "pageUrl": "https://example.com",
                        "issues": []
                    }
                ],
                "teaserUrl": "https://example.com/teaser.png"
            }
            """) as JsonObject;

            _mockScanner
                .Setup(x => x.ScanFivePagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockScanResult!);

            _mockPdfService
                .Setup(x => x.GenerateAndUploadPdf(It.IsAny<string>(), It.IsAny<JsonObject>()))
                .ReturnsAsync("https://example.com/report.pdf");
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}