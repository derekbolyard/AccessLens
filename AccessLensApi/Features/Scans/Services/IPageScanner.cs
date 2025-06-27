using AccessLensApi.Features.Scans.Models;

namespace AccessLensApi.Features.Scans.Services
{
    public interface IPageScanner
    {
        Task<PageScanResult> ScanPageAsync(
            string url,
            bool captureScreenshot,
            CancellationToken ct = default);
    }
}
