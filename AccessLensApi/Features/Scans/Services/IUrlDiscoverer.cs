using AccessLensApi.Features.Scans.Models;

namespace AccessLensApi.Features.Scans.Services
{
    /// <summary>Yields absolute page URLs belonging to <c>root</c>.</summary>
    public interface IUrlDiscoverer
    {
        IAsyncEnumerable<string> DiscoverAsync(
            Uri root,
            Models.ScanOptions options,
            CancellationToken ct = default);
    }
}
