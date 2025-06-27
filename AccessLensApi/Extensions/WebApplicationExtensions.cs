using AccessLensApi.Data;
using AccessLensApi.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

namespace AccessLensApi.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            // Error handling
            app.UseMiddleware<ErrorHandlingMiddleware>();

            // Security headers
            app.UseMiddleware<SecurityHeadersMiddleware>();

            // Forwarded headers for reverse proxy support
            app.UseForwardedHeaders();

            // Session and routing
            app.UseSession();
            app.UseRouting();
            app.UseStaticFiles();

            // Request logging with health check filtering
            app.UseSerilogRequestLogging(opts =>
            {
                opts.GetLevel = (httpCtx, elapsed, ex) =>
                    httpCtx.Request.Path.StartsWithSegments("/api/health")
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information;
            });

            // Custom request/response logging for audit trails
            if (!app.Environment.IsDevelopment())
            {
                app.UseMiddleware<RequestLoggingMiddleware>();
            }

            // Development-only middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // CORS
            app.UseCors();

            // Rate limiting
            app.UseMiddleware<RateLimitMiddleware>();

            // HTTPS redirection
            app.UseHttpsRedirection();

            // Authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Database migration
            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
            }

            return app;
        }

        public static WebApplication ConfigureEndpoints(this WebApplication app)
        {
            // API endpoints
            app.MapControllers();

            // CSRF token endpoint
            app.MapGet("/api/auth/csrf", (IAntiforgery anti, HttpContext ctx) =>
            {
                var tok = anti.GetAndStoreTokens(ctx);
                return Results.Text(tok.RequestToken!);
            });

            // Health check endpoint
            app.MapGet("/api/health", () => Results.Json(new { 
                status = "ok", 
                timestamp = DateTime.UtcNow,
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            }));

            // Fallback to serve the frontend
            app.MapFallbackToFile("index.html");

            return app;
        }
    }
}
