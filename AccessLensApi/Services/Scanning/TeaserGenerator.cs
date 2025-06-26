using AccessLensApi.Models.ScannerDtos;
using AccessLensApi.Storage;
using AccessLensApi.Utilities;

namespace AccessLensApi.Services.Scanning
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

        public async Task<Teaser?> TryGenerateAsync(
            PageScanResult firstPage,
            CancellationToken ct = default)
        {
            if (firstPage.Screenshot is null) return null;

            var axeObj = firstPage.AxeJson;
            var violations = axeObj["violations"]!.AsArray();

            int crit = violations.Count(v => v?["impact"]?.ToString() == "critical");
            int serious = violations.Count(v => v?["impact"]?.ToString() == "serious");
            int moderate = violations.Count(v => v?["impact"]?.ToString() == "moderate");
            int score = A11yScore.From(axeObj);

            // overlay
            (byte[] raw, bool _) = (firstPage.Screenshot, true);
            byte[] final = TeaserOverlay.AddOverlay(raw, score, crit, serious, moderate);

            string key = $"teasers/{Guid.NewGuid():N}.png";
            await _storage.UploadAsync(key, final, ct);

            _log.LogInformation("Teaser generated for {Url} score {Score}", firstPage.Url, score);

            return new Teaser(
                Url: _storage.GetPresignedUrl(key, TimeSpan.FromDays(7)),
                TopIssues: violations
                    .OrderBy(v => Models.ImpactPriority.Get(v?["impact"]?.ToString()))
                    .ThenBy(v => v?["id"]?.ToString())
                    .Take(5)
                    .Select(v => new TopIssue(
                        Severity: v?["impact"]?.ToString()?.ToUpperInvariant() ?? "UNKNOWN",
                        Text: v?["help"]?.ToString() ?? string.Empty))
                    .ToList());
        }
    }
}
