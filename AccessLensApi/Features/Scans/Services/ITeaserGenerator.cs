using AccessLensApi.Features.Scans.Models;

namespace AccessLensApi.Features.Scans.Services
{
    public interface ITeaserGenerator
    {
        Task<TeaserDto?> TryGenerateAsync(
            PageScanResult firstPage,
            List<PageResult> allPages,
            int? overrideScore = null,
            CancellationToken ct = default);
    }
}
