using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace AccessLensApi.Tests.Scanner
{
    public class AxeStaticFixturesTests : IAsyncLifetime
    {
        private IBrowser _browser = default!;

        [Theory]
        [InlineData("IMG_ALT_01_fail.html", false)]
        [InlineData("IMG_ALT_01_pass.html", true)]
        public async Task Axe_finds_expected_issues(string file, bool shouldPass)
        {
            var ctx = await _browser.NewContextAsync();
            var page = await ctx.NewPageAsync();

            var path = Path.GetFullPath(Path.Combine("Scanner/Fixtures", file))
                           .Replace("\\", "/");
            await page.GotoAsync($"file:///{path}");

            if (shouldPass)
                await page.ShouldHaveNoViolationsAsync();
            else
            {
                var result = await page.RunAxe();
                Assert.NotEmpty(result.Violations);
            }
        }

        // -------- xUnit async setup/teardown ----------
        public async Task InitializeAsync()
            => _browser = await Playwright.CreateAsync()
                                         .Result.Chromium
                                         .LaunchAsync(new() { Headless = true });

        public async Task DisposeAsync() => await _browser.DisposeAsync();
    }
}