namespace AccessLensApi.Services.Scanning
{
    /// <summary>Yields absolute page URLs belonging to <c>root</c>.</summary>
    public interface IUrlDiscoverer
    {
        IAsyncEnumerable<string> DiscoverAsync(
            Uri root,
            Features.Scans.Models.ScanOptions options,
            CancellationToken ct = default);
    }
}
