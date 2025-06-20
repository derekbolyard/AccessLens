using Microsoft.Playwright;

namespace AccessLensApi.Services.Interfaces
{
    public interface IBrowserProvider
    {
        Task<IBrowser> GetBrowserAsync();
    }
}
