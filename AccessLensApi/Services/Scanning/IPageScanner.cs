namespace AccessLensApi.Services.Scanning
{
    public interface IPageScanner
    {
        Task<PageScanResult?> ScanPageAsync(
            string url,
            bool captureScreenshot,
            CancellationToken ct = default);
    }
}
