using AccessLensApi.Features.Core.Models;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Services;
using AccessLensApi.Storage;
using AccessLensApi.Utilities;
using System.Text.Json.Nodes;

namespace AccessLensApi.Features.Scans.Services
{
    internal sealed class TeaserGenerator : ITeaserGenerator
    {
        private readonly IStorageService _storage;
        private readonly ILogger<TeaserGenerator> _log;

        public TeaserGenerator(
            IStorageService storage,
            ILogger<TeaserGenerator> log)
        {
            _storage = storage;
            _log = log;
        }

        public async Task<TeaserDto?> TryGenerateAsync(
            PageScanResult firstPage,
            List<PageResult> allPages,
            int? overrideScore = null,
            CancellationToken ct = default)
        {
            if (firstPage.Screenshot is null) return null;

            var axeObj = firstPage.AxeJson;
            var violations = axeObj?["violations"]?.AsArray() ?? new JsonArray();

            // Calculate severity counts from single page (for visual markers)
            int crit = violations.Count(v => v?["impact"]?.ToString() == "critical");
            int serious = violations.Count(v => v?["impact"]?.ToString() == "serious");
            int moderate = violations.Count(v => v?["impact"]?.ToString() == "moderate");
            
            // Use override score if provided, otherwise calculate from single page
            int score = overrideScore ?? (axeObj != null ? A11yScore.From(axeObj) : 0);

            // overlay with overall score but single-page severity counts for visual markers
            (byte[] raw, bool _) = (firstPage.Screenshot, true);
            byte[] final = TeaserOverlay.AddOverlay(raw, score, crit, serious, moderate);

            string key = $"teasers/{Guid.NewGuid():N}.png";
            await _storage.UploadAsync(key, final, ct);

            _log.LogInformation("Teaser generated for {Url} with overall score {Score} (page issues: {Crit}C/{Serious}S/{Moderate}M)", 
                firstPage.Url, score, crit, serious, moderate);

            // Aggregate top issues from all pages for better representation
            var allViolations = allPages
                .SelectMany(page => 
                {
                    try
                    {
                        return page.Issues.Select(issue => new
                        {
                            Impact = issue.Type,
                            Help = issue.Message,
                            Id = issue.Code
                        });
                    }
                    catch
                    {
                        return Enumerable.Empty<dynamic>();
                    }
                })
                .GroupBy(v => v.Id)
                .Select(g => new
                {
                    impact = g.First().Impact,
                    help = g.First().Help,
                    id = g.Key,
                    count = g.Count()
                })
                .OrderBy(v => ImpactPriority.Get(v.impact))
                .ThenByDescending(v => v.count)
                .Take(5);

            return new TeaserDto(
                Url: _storage.GetPresignedUrl(key, TimeSpan.FromDays(7)),
                TopIssues: allViolations
                    .Select(v => new TopIssue(
                        Severity: v.impact?.ToUpperInvariant() ?? "UNKNOWN",
                        Text: v.help ?? string.Empty))
                    .ToList());
        }
    }
}
