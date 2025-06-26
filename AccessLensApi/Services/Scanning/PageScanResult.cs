using AccessLensApi.Models.ScannerDtos;
using System.Text.Json.Nodes;

namespace AccessLensApi.Services.Scanning
{
    /// <summary>Internal transfer object produced by <see cref="IPageScanner"/>.</summary>
    public sealed record PageScanResult(
        string Url,
        PageResult Result,
        JsonObject AxeJson,
        byte[]? Screenshot);
}
