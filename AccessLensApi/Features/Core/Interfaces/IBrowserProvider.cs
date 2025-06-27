using Microsoft.Playwright;

namespace AccessLensApi.Features.Core.Interfaces
{
    public interface IBrowserProvider
    {
        Task<IBrowser> GetBrowserAsync();
    }
}
