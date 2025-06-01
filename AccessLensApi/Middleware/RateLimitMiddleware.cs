using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AccessLensApi.Middleware
{
    /// <summary>
    /// Simple in-memory rate limiter:
    /// Limits unverified scans to N per IP per window.
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly RateLimitingOptions _options;

        public RateLimitMiddleware(
            RequestDelegate next,
            IMemoryCache memoryCache,
            IOptions<RateLimitingOptions> options)
        {
            _next = next;
            _memoryCache = memoryCache;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Apply only to POST /api/scan/generate
            if (context.Request.Path.Equals("/api/scan/generate", StringComparison.OrdinalIgnoreCase)
                && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Rate-limit key per IP
                var cacheKey = $"ScanCount-{ip}";
                var entry = _memoryCache.GetOrCreate(cacheKey, entryOptions =>
                {
                    entryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.WindowMinutes);
                    return 0;
                });

                if (entry >= _options.MaxUnverifiedScansPerHour)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." });
                    return;
                }

                // Increment count
                _memoryCache.Set(cacheKey, entry + 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.WindowMinutes)
                });
            }

            await _next(context);
        }
    }
}
