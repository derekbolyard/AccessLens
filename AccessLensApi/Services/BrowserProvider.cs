using AccessLensApi.Services.Interfaces;
using Microsoft.Playwright;
using System;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

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
                "--js-flags=--code-range-size=128"
            }
            });
            return _browser;
        }
    }

}
