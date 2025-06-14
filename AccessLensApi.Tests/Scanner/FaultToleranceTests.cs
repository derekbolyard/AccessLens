using AccessLensApi.Services;
using AccessLensApi.Storage;
using Moq;
using RichardSzalay.MockHttp;

namespace AccessLensApi.Tests.Scanner
{
    public class FaultToleranceTests
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly MockHttpMessageHandler _mockHttpMessageHandler;
        private readonly A11yScanner _target;

        public FaultToleranceTests(Mock<IStorageService> mockStorageService, MockHttpMessageHandler mockHttpMessageHandler)
        {
            _mockStorageService = mockStorageService;
            _mockHttpMessageHandler = mockHttpMessageHandler;
            _mockHttpMessageHandler.When("https://cdnjs.cloudflare.com/*")
                .Respond("application/javascript", AxeShim.Javascript);
            _target = ScannerFactory.CreateScanner(_mockHttpMessageHandler.ToHttpClient(), _mockStorageService.Object);
        }

        [Fact(DisplayName = "Scanner skips pages that return 500")]
        [Trait("Category", "Integration")]
        public async Task Scan_skips_pages_that_return_500()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://mysite.com/*")
                    .Respond(System.Net.HttpStatusCode.InternalServerError);
            mockHttp.When("*cdnjs*").Respond("application/javascript", "axe"); // axe loads


            var json = await _target.ScanFivePagesAsync("https://mysite.com");

            Assert.Equal(0, json["totalPages"]!.GetValue<int>());
        }

        [Fact(DisplayName = "GetAxeScriptAsync throws when CDN unreachable")]
        [Trait("Category", "Integration")]
        public async Task GetAxeScript_throws_if_cdn_unreachable()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("*").Respond(System.Net.HttpStatusCode.NotFound);

            await Assert.ThrowsAsync<InvalidOperationException>(
                  () => _target.ScanFivePagesAsync("https://example.com"));
        }
    }
}
