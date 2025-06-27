namespace AccessLensApi.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Remove server information
            context.Response.Headers.Remove("Server");

            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            
            // Only add HSTS if using HTTPS
            if (context.Request.IsHttps)
            {
                context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            // Content Security Policy - adjust as needed for your application
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                     "style-src 'self' 'unsafe-inline'; " +
                     "img-src 'self' data: https:; " +
                     "font-src 'self'; " +
                     "connect-src 'self'; " +
                     "frame-ancestors 'none'";
            
            context.Response.Headers.Add("Content-Security-Policy", csp);

            await _next(context);
        }
    }
}
