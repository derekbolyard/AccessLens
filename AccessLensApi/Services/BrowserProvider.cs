using AccessLensApi.Services.Interfaces;
using Microsoft.Playwright;

namespace AccessLensApi.Services
{
    public class BrowserProvider : IBrowserProvider
    {
        private static IBrowser? _browser;
        private static IPlaywright? _playwright;

        public async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser != null) return _browser;

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Args = new[]
                {
                "--no-sandbox",
                "--disable-gpu",
                "--disable-dev-shm-usage"
            },
                ChromiumSandbox = false
            });

            return _browser;
        }
    }

}
