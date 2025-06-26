using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Models.ScannerDtos;
using System.Text.Json.Nodes;

namespace AccessLensApi.Services.Scanning
{
    public interface IA11yScanner
    {
        /// Runs a 5-page starter-tier crawl and returns a merged JSON report.
        Task<A11yScanResult> ScanFivePagesAsync(string rootUrl, CancellationToken cancellationToken = default);

        /// Runs a full site crawl and returns a comprehensive JSON report.
        Task<A11yScanResult> ScanAllPagesAsync(string rootUrl, ScanOptions? options = null, CancellationToken cancellationToken = default);
    }
}
