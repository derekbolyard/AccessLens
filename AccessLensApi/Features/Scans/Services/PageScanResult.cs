using AccessLensApi.Features.Scans.Models;
using System.Text.Json.Nodes;

namespace AccessLensApi.Features.Scans.Services
{
    /// <summary>Internal transfer object produced by <see cref="IPageScanner"/>.</summary>
    public sealed record PageScanResult(
        string Url,
        PageResult Result,
        JsonObject AxeJson,
        byte[]? Screenshot);
}
