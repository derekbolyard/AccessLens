using AccessLensApi.Features.Scans.Models;

namespace AccessLensApi.Features.Scans.Services
{
    public interface ITeaserGenerator
    {
        Task<TeaserDto?> TryGenerateAsync(
            PageScanResult firstPage,
            CancellationToken ct = default);
    }
}
