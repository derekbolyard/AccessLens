using AccessLensApi.Services.Interfaces;

namespace AccessLensApi.Services
{
    public sealed class AxeScriptProvider : IAxeScriptProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<AxeScriptProvider> _log;
        private const string Cdn =
            "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.0/axe.min.js";
        private static string? _cached;                 // per-process cache

        public AxeScriptProvider(HttpClient http, ILogger<AxeScriptProvider> log)
        {
            _http = http;
            _log = log;
        }

        public async Task<string> GetAsync(CancellationToken ct = default)
        {
            if (_cached is not null) return _cached;
            try
            {
                var asm = typeof(AxeScriptProvider).Assembly;
                const string id = "AccessLensApi.Assets.axe-core-4.9.0.min.js";

                await using var stream = asm.GetManifestResourceStream(id)
                    ?? throw new InvalidOperationException(
                        $"Embedded axe-core ({id}) not found in assembly.");

                using var reader = new StreamReader(stream);
                _cached = await reader.ReadToEndAsync();
                if(string.IsNullOrWhiteSpace(_cached)) throw new InvalidOperationException("Embedded axe-core is empty.");
                return _cached;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "axe-core Embedded not found, using CDN");
            }

            // ② Fallback to embedded
            var resp = await _http.GetAsync(Cdn, ct);
            if (resp.IsSuccessStatusCode)
            {
                _cached = await resp.Content.ReadAsStringAsync(ct);
                return _cached;
            }

            throw new InvalidOperationException($"Failed to load axe-core from embedded resource && CDN: {Cdn} (status code: {resp.StatusCode})");
        }
    }
}
