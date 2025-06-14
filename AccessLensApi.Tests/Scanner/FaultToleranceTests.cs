using AccessLensApi.Services;
using AccessLensApi.Storage;
using AccessLensApi.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RichardSzalay.MockHttp;

namespace AccessLensApi.Tests.Scanner
{
    public class FaultToleranceTests
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly MockHttpMessageHandler _mockHttpMessageHandler;
        private readonly A11yScanner _target;

        public FaultToleranceTests()
        {
            _mockStorageService = new Mock<IStorageService>();
            _mockHttpMessageHandler = new MockHttpMessageHandler();
            _target = ScannerFactory.CreateScanner(_mockHttpMessageHandler.ToHttpClient(), _mockStorageService.Object);
        }

        [Fact(DisplayName = "Scanner skips pages that return 500")]
        [Trait("Category", "Integration")]
        public async Task Scan_skips_pages_that_return_500()
        {
            _mockHttpMessageHandler.When("https://mysite.com/*")
                    .Respond(System.Net.HttpStatusCode.InternalServerError);

            var json = await _target.ScanFivePagesAsync("https://mysite.com");

            Assert.Equal(0, json["totalPages"]!.GetValue<int>());
        }
    }
}
