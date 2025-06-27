using AccessLensApi.Features.Core.Interfaces;
using Microsoft.Playwright;
using System.Threading;

namespace AccessLensApi.Features.Scans.Services
{
    /// <summary>
    /// One per process. Hands out a shared IBrowser; callers must create their own
    /// contexts:  var ctx = await browser.NewContextAsync();
    /// </summary>
    public class BrowserProvider : IBrowserProvider
    {
        private readonly IPlaywright _pw;
        private IBrowser? _browser;
        private readonly SemaphoreSlim _bootLock = new(1, 1);

        public BrowserProvider(IPlaywright pw) => _pw = pw;

        public async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser?.IsConnected == true) return _browser;

            await _bootLock.WaitAsync();
            try
            {
                if (_browser?.IsConnected == true) return _browser; // another thread might have done it
                _browser = await _pw.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    ChromiumSandbox = false,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    }
                });
                return _browser;
            }
            finally
            {
                _bootLock.Release();
            }
        }
    }
}
