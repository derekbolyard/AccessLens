using System.Text;

namespace AccessLensApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for health checks and static files
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var correlationId = Guid.NewGuid().ToString();
            context.Items["CorrelationId"] = correlationId;

            // Log request
            await LogRequestAsync(context.Request, correlationId);

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                await LogResponseAsync(context.Response, correlationId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
        }

        private async Task LogRequestAsync(HttpRequest request, string correlationId)
        {
            var logData = new
            {
                CorrelationId = correlationId,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                UserAgent = request.Headers["User-Agent"].FirstOrDefault(),
                UserEmail = request.HttpContext.User?.FindFirst("email")?.Value,
                ClientIp = GetClientIpAddress(request),
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("HTTP Request: {@RequestLog}", logData);

            // Log request body for POST/PUT requests (excluding file uploads)
            if (ShouldLogRequestBody(request))
            {
                var body = await ReadRequestBodyAsync(request);
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogDebug("Request Body [{CorrelationId}]: {Body}", correlationId, body);
                }
            }
        }

        private async Task LogResponseAsync(HttpResponse response, string correlationId, long elapsedMs)
        {
            var logData = new
            {
                CorrelationId = correlationId,
                StatusCode = response.StatusCode,
                ElapsedMs = elapsedMs,
                Timestamp = DateTime.UtcNow
            };

            if (response.StatusCode >= 400)
            {
                _logger.LogWarning("HTTP Response Error: {@ResponseLog}", logData);
                
                // Log response body for errors
                var responseBody = await ReadResponseBodyAsync(response);
                if (!string.IsNullOrEmpty(responseBody))
                {
                    _logger.LogDebug("Error Response Body [{CorrelationId}]: {Body}", correlationId, responseBody);
                }
            }
            else
            {
                _logger.LogInformation("HTTP Response: {@ResponseLog}", logData);
            }
        }

        private static bool ShouldSkipLogging(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant();
            return pathValue?.Contains("/health") == true ||
                   pathValue?.Contains("/swagger") == true ||
                   pathValue?.Contains("/css/") == true ||
                   pathValue?.Contains("/js/") == true ||
                   pathValue?.Contains("/img/") == true ||
                   pathValue?.Contains("/favicon") == true;
        }

        private static bool ShouldLogRequestBody(HttpRequest request)
        {
            return (request.Method == "POST" || request.Method == "PUT") &&
                   request.ContentType?.Contains("application/json") == true &&
                   request.ContentLength < 10000; // Don't log large payloads
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[request.ContentLength ?? 0];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;
            return Encoding.UTF8.GetString(buffer);
        }

        private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }

        private static string GetClientIpAddress(HttpRequest request)
        {
            return request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
                   request.Headers["X-Real-IP"].FirstOrDefault() ??
                   request.HttpContext.Connection.RemoteIpAddress?.ToString() ??
                   "Unknown";
        }
    }
}
