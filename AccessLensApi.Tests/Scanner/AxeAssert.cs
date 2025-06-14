using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using FluentAssertions;
using Microsoft.Playwright;

namespace AccessLensApi.Tests.Scanner
{
    public static class AxeAssert
    {
        public static async Task ShouldHaveNoViolationsAsync(
             this IPage page, AxeRunOptions? opts = null)
        {
            AxeResult result = await page.RunAxe(opts);
            result.Violations.Should().BeEmpty();
        }
    }
}
