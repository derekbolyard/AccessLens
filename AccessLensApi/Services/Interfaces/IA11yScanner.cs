using AccessLensApi.Features.Scans.Models;
using System.Text.Json.Nodes;

namespace AccessLensApi.Services.Interfaces
{
    public interface IA11yScanner
    {
        /// Runs a 5-page starter-tier crawl and returns a merged JSON report.
        Task<JsonObject> ScanFivePagesAsync(string rootUrl, CancellationToken cancellationToken = default);

        /// Runs a full site crawl and returns a comprehensive JSON report.
        Task<JsonObject> ScanAllPagesAsync(string rootUrl, ScanOptions? options = null, CancellationToken cancellationToken = default);
    }
}
