using AccessLensApi.Services;
using AccessLensApi.Storage;
using Moq;
using RichardSzalay.MockHttp;
using System.Text.Json;

namespace AccessLensApi.Tests.Scanner
{
    public class ContractTest
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly MockHttpMessageHandler _mockHttpMessageHandler;
        private readonly A11yScanner _target;
        public ContractTest()
        {
            _mockStorageService = new Mock<IStorageService>();
            _mockHttpMessageHandler = new MockHttpMessageHandler();
            _mockHttpMessageHandler.When("*cdnjs.cloudflare.com/*")
                .Respond("application/javascript", AxeShim.Javascript);
            _target = ScannerFactory.CreateScanner(_mockHttpMessageHandler.ToHttpClient(), _mockStorageService.Object);
        }

        private static readonly JsonSerializerOptions _opts = new()
        { WriteIndented = true };

        [Fact(Skip = "Runs nightly via CI cron")]
        public async Task Frozen_BAD_before_site_matches_baseline()
        {
            var json = await _target.ScanFivePagesAsync(
                           "https://accesslens-bad-before-demo.netlify.app/");
            var today = JsonSerializer.Serialize(json, _opts);
            var baseL = await File.ReadAllTextAsync("E2E/baseline.json");

            Assert.Equal(baseL, today);
        }
    }
}
