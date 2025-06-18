// Program.cs – AccessLens API (JWT via HttpOnly cookie, CSRF‑ready)
// ---------------------------------------------------------------
using AccessLensApi.Data;
using AccessLensApi.Features.Auth;
using AccessLensApi.Middleware;
using AccessLensApi.Services;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using Amazon;
using Amazon.S3;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using QuestPDF.Infrastructure;
using Serilog;
using Stripe;
using System.Data;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// 1️⃣  SECURITY SERVICES
// --------------------------------------------------
// Antiforgery for SPA (Angular will read the XSRF‑TOKEN cookie and echo header)
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "XSRF-TOKEN";
    o.Cookie.SameSite = SameSiteMode.Lax;   // works cross‑sub‑domain
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Jwt Authentication – single scheme reading from HttpOnly cookie *or* Authorization header.
var jwtSecret = builder.Configuration["MagicJwt:SecretKey"] ??
                throw new InvalidOperationException("MagicJwt SecretKey is required");
var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication("MagicJwt")
    .AddJwtBearer("MagicJwt", opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidIssuer = "accesslens",
            ValidAudience = "session",   // long‑lived session token
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Pull token from cookie when header isn’t present
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

// --------------------------------------------------
// 2️⃣  INFRA & LIBS (unchanged from your original)
// --------------------------------------------------
QuestPDF.Settings.License = LicenseType.Community;
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .ReadFrom.Configuration(ctx.Configuration));

var configuration = builder.Configuration;
var sqliteConnString = configuration.GetConnectionString("SqliteConnection")
                        ?? Environment.GetEnvironmentVariable("SQLITE_CONNECTION_STRING")
                        ?? "Data Source=accesslens.db";

builder.Services.AddTransient<IDbConnection>(_ =>
{
    var conn = new SqliteConnection(sqliteConnString);
    conn.Open();
    return conn;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnString));

builder.Services.Configure<PlaywrightOptions>(configuration.GetSection("Playwright"));
var skipPwInstall = Environment.GetEnvironmentVariable("SKIP_PLAYWRIGHT_INSTALL");
if (skipPwInstall != "1")
{
    await Task.Run(() =>
    {
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
            throw new Exception($"Playwright install failed (exit code {exitCode})");
    });
}

builder.Services.AddSingleton<IPlaywright>(_ =>
{
    return Task.Run(async () =>
    {
        var playwright = await Playwright.CreateAsync();
        var browsersPath = configuration["Playwright:BrowsersPath"];
        if (!string.IsNullOrEmpty(browsersPath))
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browsersPath);
        return playwright;
    }).GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IBrowser>(sp =>
{
    var pwOpts = sp.GetRequiredService<IOptions<PlaywrightOptions>>().Value;
    var pw = sp.GetRequiredService<IPlaywright>();
    var launch = new BrowserTypeLaunchOptions { Headless = pwOpts.Headless, Args = pwOpts.Args };
    return pw[pwOpts.Browser].LaunchAsync(launch).GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IAxeScriptProvider, AxeScriptProvider>();
builder.Services.AddSingleton<IA11yScanner, A11yScanner>();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddSingleton<IMagicTokenService, MagicTokenService>();

var awsRegion = RegionEndpoint.GetBySystemName(configuration["AWS:Region"]);
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(awsRegion));

#if DEBUG
builder.Services.AddSingleton<IEmailService, LocalEmailService>();
#else
builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
#endif
builder.Services.AddSingleton<IStorageService, S3StorageService>();

StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"] ??
                             Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

builder.Services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimitingOptions"));
builder.Services.Configure<CaptchaOptions>(configuration.GetSection("Captcha"));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IRateLimiter, RateLimiterService>();
builder.Services.AddScoped<ICreditManager, CreditManager>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:4200", "https://localhost:4200", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// --------------------------------------------------
// 3️⃣  BUILD APP / PIPELINE
// --------------------------------------------------
var app = builder.Build();

app.UseSession();
app.UseRouting();

// Static + forwarded headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<RateLimitMiddleware>();
app.UseHttpsRedirection();

// 👇  Auth before anything that needs User
app.UseAuthentication();
app.UseAuthorization();

// EF migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseSerilogRequestLogging();

// Map controllers (your Magic‑link endpoint lives in its own controller)
app.MapControllers();
app.MapGet("/api/auth/csrf", (IAntiforgery anti, HttpContext ctx) =>
{
    var tokens = anti.GetAndStoreTokens(ctx);   // sets XSRF-TOKEN cookie
    return Results.Text(tokens.RequestToken!);  // send the matching request token
});

// Serve Angular front-end for non-API routes
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
