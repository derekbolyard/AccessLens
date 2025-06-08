using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using AccessLensApi.Middleware;
using AccessLensApi.Services.Interfaces;

namespace AccessLensApi.Services
{
    public class RateLimiterService : IRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly RateLimitingOptions _options;
        private readonly SemaphoreSlim _concurrent;

        public RateLimiterService(IMemoryCache cache, IOptions<RateLimitingOptions> options)
        {
            _cache = cache;
            _options = options.Value;
            _concurrent = new SemaphoreSlim(_options.MaxConcurrentScans, _options.MaxConcurrentScans);
        }

        public async Task<bool> TryAcquireStarterAsync(string ip, string email, bool verified)
        {
            if (!await _concurrent.WaitAsync(0))
                return false;

            try
            {
                var now = DateTime.UtcNow;

                var ipKey = $"starter-ip-{ip}";
                var ipCount = _cache.Get<int>(ipKey);
                if (ipCount >= _options.MaxStarterScansPerHour)
                    return Fail();
                _cache.Set(ipKey, ipCount + 1, TimeSpan.FromHours(1));

                if (!verified)
                {
                    var uvKey = $"starter-uv-{ip}";
                    var uvCount = _cache.Get<int>(uvKey);
                    if (uvCount >= _options.MaxUnverifiedScansPerHour)
                        return Fail();
                    _cache.Set(uvKey, uvCount + 1, TimeSpan.FromHours(1));
                }

                var emailKey = $"starter-email-{email}";
                var emailCount = _cache.Get<int>(emailKey);
                if (emailCount >= _options.MaxScansPerEmailPerDay)
                    return Fail();
                _cache.Set(emailKey, emailCount + 1, TimeSpan.FromDays(1));

                return true;
            }
            catch
            {
                _concurrent.Release();
                throw;
            }

            bool Fail()
            {
                _concurrent.Release();
                return false;
            }
        }

        public void ReleaseStarter()
        {
            _concurrent.Release();
        }
    }
}
