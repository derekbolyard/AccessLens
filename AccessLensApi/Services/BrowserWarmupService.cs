using AccessLensApi.Services.Interfaces;

namespace AccessLensApi.Services
{
    public class BrowserWarmupService : IHostedService
    {
        private readonly IBrowserProvider _provider;
        private readonly ILogger<BrowserWarmupService> _logger;

        public BrowserWarmupService(IBrowserProvider provider, ILogger<BrowserWarmupService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("[Startup] Warming up browser...");
                var browser = await _provider.GetBrowserAsync();
                _logger.LogInformation("[Startup] Browser warmed up successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Browser warmup failed.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
