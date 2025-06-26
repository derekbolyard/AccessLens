using AccessLensApi.Models.ScannerDtos;

namespace AccessLensApi.Services.Scanning
{
    public interface ITeaserGenerator
    {
        Task<Teaser?> TryGenerateAsync(
            PageScanResult firstPage,
            CancellationToken ct = default);
    }
}
