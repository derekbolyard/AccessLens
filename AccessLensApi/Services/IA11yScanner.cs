using System.Text.Json.Nodes;

namespace AccessLensApi.Services
{
    public interface IA11yScanner
    {
        /// Runs a 5-page starter-tier crawl and returns a merged JSON report.
        Task<JsonArray> ScanFivePagesAsync(string rootUrl);
    }
}
