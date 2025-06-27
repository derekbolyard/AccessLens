using AccessLensApi.Config;
using AccessLensApi.Data;
using AccessLensApi.Features.Auth;
using AccessLensApi.Features.Core.Interfaces;
using AccessLensApi.Features.Core.Services;
using AccessLensApi.Features.Payments.Services;
using AccessLensApi.Features.Reports;
using AccessLensApi.Features.Reports.Repositories;
using AccessLensApi.Features.Scans.Services;
using AccessLensApi.Middleware;
using AccessLensApi.Storage;
using AccessLensApi.Common.Repositories;
using AccessLensApi.Common.Services;
using AccessLensApi.Common.Validation;
using AccessLensApi.Common.Jobs;
using AccessLensApi.Workers;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using System.Data;
using System.Text;

namespace AccessLensApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section).ValidateDataAnnotations()
                .Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>()
                .AddOptions<S3Options>().BindConfiguration(S3Options.Section).ValidateDataAnnotations()
                .Services.AddSingleton<IValidateOptions<S3Options>, S3OptionsValidator>()
                .AddOptions<AccessLensApi.Config.PlaywrightOptions>().BindConfiguration(AccessLensApi.Config.PlaywrightOptions.Section)
                .Services.AddSingleton<IValidateOptions<AccessLensApi.Config.PlaywrightOptions>, PlaywrightOptionsValidator>()
                .AddOptions<MinioOptions>().BindConfiguration(MinioOptions.Section)
                .Services
                .AddOptions<RateLimitingOptions>().BindConfiguration("RateLimiting");

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
                      ?? throw new InvalidOperationException("Jwt settings missing");

            services.AddAuthentication("JwtCookie")
                .AddJwtBearer("JwtCookie", opts =>
                {
                    var keyBytes = Encoding.UTF8.GetBytes(jwt.SecretKey);

                    opts.TokenValidationParameters = new()
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    opts.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            ctx.Token ??= ctx.Request.Cookies["access_token"];
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(o =>
                o.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser()));

            return services;
        }

        public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
        {
            services
                .AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString))
                .AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));

            return services;
        }

        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            // S3/MinIO Storage
            var minio = configuration.GetSection(MinioOptions.Section).Get<MinioOptions>()
                      ?? throw new InvalidOperationException("Minio settings missing");

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<S3Options>>().Value;
                if (string.IsNullOrEmpty(opts.ServiceUrl))
                    return new AmazonS3Client(RegionEndpoint.GetBySystemName(opts.Region));

                var cfg = new AmazonS3Config
                {
                    ServiceURL = opts.ServiceUrl,
                    ForcePathStyle = true,
                    AuthenticationRegion = opts.Region
                };
                return new AmazonS3Client(minio.User, minio.Password, cfg);
            });

            services.AddSingleton<IStorageService, S3StorageService>();

            // Email Service
        #if DEBUG
            services.AddSingleton<IEmailService, LocalEmailService>();
         #else
            services.AddSingleton<IEmailService, SendGridEmailService>();
         #endif

            // Playwright
            services.AddSingleton<IPlaywright>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<AccessLensApi.Config.PlaywrightOptions>>().Value;
                Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", opts.BrowsersPath);
                return Playwright.CreateAsync().GetAwaiter().GetResult();
            });

            return services;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Repositories and Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IReportRepository, ReportRepository>();

            // Core services
            services.AddSingleton<IBrowserProvider, BrowserProvider>();
            services.AddSingleton<IAxeScriptProvider, AxeScriptProvider>();
            services.AddSingleton<IUrlDiscoverer, UrlDiscoverer>();
            services.AddSingleton<IPageScanner, PageScanner>();
            services.AddSingleton<ITeaserGenerator, TeaserGenerator>();
            services.AddScoped<IA11yScanner, A11yScanner>();
            services.AddSingleton<IMagicTokenService, MagicTokenService>();
            services.AddSingleton<IRateLimiter, RateLimiterService>();
            services.AddScoped<ICreditManager, CreditManager>();
            services.AddTransient<IReportBuilder, ReportBuilder>();

            // Common services
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<INotificationService, EmailNotificationService>();

            // Job Queue and Workers
            services.AddSingleton<IJobQueue<ScanJob>, InMemoryJobQueue<ScanJob>>();

            // Hosted services
            services.AddHostedService<BrowserWarmupService>();
            services.AddHostedService<ScanWorkerService>();

            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                services.AddCors(o =>
                    o.AddDefaultPolicy(p => p
                        .WithOrigins(Enumerable.Range(1024, 64511)
                                .Select(port => $"http://localhost:{port}")
                                .ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()));
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.WithOrigins("https://getaccesslens.com", "https://www.getaccesslens.com")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });
            }

            return services;
        }

        public static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            services.AddAntiforgery(o =>
            {
                o.HeaderName = "X-CSRF-TOKEN";
                o.Cookie.Name = "XSRF-TOKEN";
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            return services;
        }
    }
}
