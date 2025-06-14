using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Services;
using AccessLensApi.Storage;
using Moq;
using RichardSzalay.MockHttp;

namespace AccessLensApi.Tests.Scanner
{
    public class StorageFailureTests
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly MockHttpMessageHandler _mockHttpMessageHandler;
        private readonly A11yScanner _target;
        public StorageFailureTests()
        {
            _mockStorageService = new Mock<IStorageService>();
            _mockHttpMessageHandler = new MockHttpMessageHandler();
            _target = ScannerFactory.CreateScanner(_mockHttpMessageHandler.ToHttpClient(), _mockStorageService.Object);
        }

        [Fact(DisplayName = "Teaser upload failure does not abort scan")]
        public async Task Scanner_survives_teaser_upload_error()
        {
            // storage mock that throws on UploadAsync

            _mockStorageService.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("S3 outage"));
            _mockStorageService.Setup(s => s.GetPresignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                       .Returns("https://dummy/teaser.png");

            // fake HTML + axe CDN responses

            _mockHttpMessageHandler.When("https://site.com/*")
                .Respond("text/html", "<html><h1>Hello</h1></html>");
            _mockHttpMessageHandler.When("*cdnjs*").Respond("application/javascript", "/* axe-core */");

            var json = await _target.ScanFivePagesAsync("https://site.com");

            Assert.Equal(1, json["totalPages"]!.GetValue<int>());
            Assert.Equal(string.Empty, json["teaserUrl"]!.GetValue<string>());
        }
    }
}
