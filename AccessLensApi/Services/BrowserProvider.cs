using AccessLensApi.Services.Interfaces;
using Microsoft.Playwright;

namespace AccessLensApi.Services
{
    public class BrowserProvider : IBrowserProvider
    {
        private static IBrowser? _browser;
        private static IPlaywright? _pw;
        public BrowserProvider(IPlaywright pw) => _pw = pw;

        public async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser is { IsConnected: true }) return _browser;

            _browser = await _pw.Chromium.LaunchAsync(new()
            {
                Headless = true,
                ChromiumSandbox = false,
                Args = new[]
                {
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--no-zygote"
            }
            });
            return _browser;
        }
    }

}
