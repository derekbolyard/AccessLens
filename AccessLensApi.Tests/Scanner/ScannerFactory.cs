using AccessLensApi.Services;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Moq;
using RichardSzalay.MockHttp;

namespace AccessLensApi.Tests.Scanner
{

    internal static class ScannerFactory
    {
        internal static A11yScanner CreateScanner(
            HttpClient? httpClient = null,
            IStorageService? storage = null,
            bool generateTeaser = false,
            ILogger<A11yScanner>? logger = null,
            IAxeScriptProvider? axe = null)
        {
            // —— Playwright browser ——
            var browser = Playwright.CreateAsync().GetAwaiter().GetResult()
                                    .Chromium.LaunchAsync(new() { Headless = true })
                                    .GetAwaiter().GetResult();

            // —— stub Http —— 
            if (httpClient is null)
            {
                var mock = new MockHttpMessageHandler();
                mock.When("*cdnjs*").Respond("application/javascript", AxeShim.Javascript);
                mock.When("*").Respond("text/html", "<html></html>");
                httpClient = mock.ToHttpClient();
            }

            // —— stub storage —— 
            if (storage is null)
            {
                var storageMock = new Mock<IStorageService>();
                storageMock.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);
                storageMock.Setup(s => s.GetPresignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                           .Returns(string.Empty);
                storage = storageMock.Object;
            }

            if (axe == null)
            {
                var axeMock = new Mock<IAxeScriptProvider>();
                axeMock.Setup(a => a.GetAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(AxeShim.Javascript);
                axe = axeMock.Object;
            }

            // —— stub logger —— 
            logger ??= Mock.Of<ILogger<A11yScanner>>();

            return new A11yScanner(browser, storage, logger, httpClient, axe);
        }

        internal static A11yScanner BuildScanner(
            IBrowser browser,
            HttpClient http,
            IAxeScriptProvider axe,
            IStorageService? storage = null,
            ILogger<A11yScanner>? log = null
            )
        {
            return new A11yScanner(
                browser,
                storage ?? new InMemoryStorage(),
                log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<A11yScanner>.Instance,
                http,
                axe);
        }
    }
}
