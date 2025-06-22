using AccessLensApi.Config;
using AccessLensApi.Data;
using AccessLensApi.Features.Auth;
using AccessLensApi.Middleware;
using AccessLensApi.Services;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    // Important: clear the defaults so we don’t block Fly’s dynamic edge IPs
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Configuration.AddEnvironmentVariables();
if (!builder.Environment.IsDevelopment())
{
    Environment.SetEnvironmentVariable("DEBUG", "pw:browser*");
    Environment.SetEnvironmentVariable("PLAYWRIGHT_LOG", "1");
}

// ------------------------------------------------------------------
// 1️⃣  CONFIG + LOGGING
// ------------------------------------------------------------------
builder.Services
    .AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section).ValidateDataAnnotations()
    .Services
    .AddOptions<S3Options>().BindConfiguration(S3Options.Section).ValidateDataAnnotations()
    .Services
    .AddOptions<AccessLensApi.Config.PlaywrightOptions>().BindConfiguration(AccessLensApi.Config.PlaywrightOptions.Section)
    .Services
    .AddOptions<MinioOptions>().BindConfiguration(MinioOptions.Section);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .MinimumLevel.Information()
        .WriteTo.Console());

// ------------------------------------------------------------------
// 2️⃣  SECURITY (Antiforgery + JWT)
// ------------------------------------------------------------------
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "XSRF-TOKEN";
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt settings missing");

builder.Services.AddAuthentication("JwtCookie")
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

builder.Services.AddAuthorization(o =>
    o.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser()));

// ------------------------------------------------------------------
// 3️⃣  DB + Dapper connection
// ------------------------------------------------------------------
var sqliteConn = builder.Configuration.GetConnectionString("Sqlite");
builder.Services
    .AddDbContext<ApplicationDbContext>(o => o.UseSqlite(sqliteConn))
    .AddTransient<IDbConnection>(_ =>
    {
        var c = new SqliteConnection(sqliteConn);
        c.Open();
        return c;
    });

// ------------------------------------------------------------------
// 4️⃣  S3 / MinIO client
// ------------------------------------------------------------------
var minio = builder.Configuration.GetSection(MinioOptions.Section).Get<MinioOptions>()
          ?? throw new InvalidOperationException("Minio settings missing");
builder.Services.AddSingleton<IAmazonS3>(sp =>
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

builder.Services.AddSingleton<IStorageService, S3StorageService>();
#if DEBUG
builder.Services.AddSingleton<IEmailService, LocalEmailService>();
#else
builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
#endif

// ------------------------------------------------------------------
// 5️⃣  Playwright (optional install)
// ------------------------------------------------------------------
builder.Services.AddSingleton<IPlaywright>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AccessLensApi.Config.PlaywrightOptions>>().Value;
    Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", opts.BrowsersPath);

    return Playwright.CreateAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IBrowserProvider, BrowserProvider>();
builder.Services.AddHostedService<BrowserWarmupService>();

// ------------------------------------------------------------------
// 6️⃣  Domain services, rate lim, etc.
// ------------------------------------------------------------------
builder.Services.AddSingleton<IAxeScriptProvider, AxeScriptProvider>();
builder.Services.AddSingleton<IA11yScanner, A11yScanner>();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddSingleton<IMagicTokenService, MagicTokenService>();

builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IRateLimiter, RateLimiterService>();
builder.Services.AddScoped<ICreditManager, CreditManager>();
builder.Services.AddMemoryCache();

// MVC, Swagger, CORS, Session
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:4200", "https://localhost:4200", "https://localhost:3000","http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
}
else
{
    builder.Services.AddCors(options =>
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



builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// ------------------------------------------------------------------
// 7️⃣  PIPELINE
// ------------------------------------------------------------------
var app = builder.Build();

app.UseExceptionHandler(a => a.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType = "application/json";
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    Log.Error(ex, "Unhandled exception");
    await ctx.Response.WriteAsJsonAsync(new { error = "Internal server error" });
}));

app.UseForwardedHeaders();

app.UseSession();
app.UseRouting();
app.UseStaticFiles();

app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (httpCtx, elapsed, ex) =>
        httpCtx.Request.Path.StartsWithSegments("/api/health")
            ? LogEventLevel.Debug      // will be dropped by the console sink
            : LogEventLevel.Information;
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors();


app.UseMiddleware<RateLimitMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}

app.UseSerilogRequestLogging();

app.MapControllers();
app.MapGet("/api/auth/csrf", (IAntiforgery anti, HttpContext ctx) =>
{
    var tok = anti.GetAndStoreTokens(ctx);
    return Results.Text(tok.RequestToken!);
});
app.MapGet("/api/health", () => Results.Json(new { status = "ok" }));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
