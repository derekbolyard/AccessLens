using AccessLensApi.Data;
using AccessLensApi.Middleware;
using AccessLensApi.Services;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Storage;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using QuestPDF.Infrastructure;
using Serilog;
using Stripe;
using System.Data;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"          // ← set here, not later
});

//builder.Configuration
//    .SetBasePath(builder.Environment.ContentRootPath)
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .AddJsonFile(
//        $"appsettings.{builder.Environment.EnvironmentName}.json",
//        optional: true,
//        reloadOnChange: true
//    )
//    .AddEnvironmentVariables()
//    .AddCommandLine(args);

QuestPDF.Settings.License = LicenseType.Community;
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .ReadFrom.Configuration(ctx.Configuration)
);

var configuration = builder.Configuration;
var sqliteConnString = builder.Configuration.GetConnectionString("SqliteConnection")
                        ?? Environment.GetEnvironmentVariable("SQLITE_CONNECTION_STRING")
                        ?? "Data Source=accesslens.db";

// (2) Register a transient IDbConnection that opens a SqliteConnection
builder.Services.AddTransient<IDbConnection>(sp =>
{
    var conn = new SqliteConnection(sqliteConnString);
    conn.Open();
    return conn;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnString));

builder.Services
    // 1) bind options from config
    .Configure<PlaywrightOptions>(builder.Configuration.GetSection("Playwright"));
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

builder.Services.AddSingleton<IPlaywright>(sp =>
{
    // Use Task.Run to create a synchronous wrapper around the async operation
    return Task.Run(async () =>
    {
        var playwright = await Playwright.CreateAsync();
        // Set custom browsers path if configured
        var browsersPath = configuration["Playwright:BrowsersPath"];
        if (!string.IsNullOrEmpty(browsersPath))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browsersPath);
        }
        return playwright;
    }).GetAwaiter().GetResult();
});
builder.Services.AddSingleton<IBrowser>(sp =>
{
    var pwOpts = sp.GetRequiredService<IOptions<PlaywrightOptions>>().Value;
    var pw = sp.GetRequiredService<IPlaywright>();

    var launch = new BrowserTypeLaunchOptions
    {
        Headless = pwOpts.Headless,
        Args = pwOpts.Args
    };

    return pw[pwOpts.Browser].LaunchAsync(launch)
                             .GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IA11yScanner, A11yScanner>();
builder.Services.AddSingleton<IPdfService, PdfService>();

var awsRegion = RegionEndpoint.GetBySystemName(configuration["AWS:Region"]);
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    return new AmazonS3Client(awsRegion);
});
//builder.Services.AddSingleton<IAmazonSimpleEmailService>(sp =>
//{
//    var awsCreds = new EnvironmentVariablesAWSCredentials();
//    return new AmazonSimpleEmailServiceClient(awsCreds, awsRegion);
//});

builder.Services.AddSingleton<IEmailService, GmailEmailService>();

//#if DEBUG
//builder.Services.AddSingleton<IStorageService, LocalStorage>();
//#else
builder.Services.AddSingleton<IStorageService, S3StorageService>();
//#endif

StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
                         ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
builder.Services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimitingOptions"));
builder.Services.Configure<CaptchaOptions>(configuration.GetSection("Captcha"));

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IRateLimiter, RateLimiterService>();

builder.Services.AddScoped<ICreditManager, CreditManager>();
//builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseSession();
app.UseRouting();
// Apply pending EF migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseSerilogRequestLogging();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseMiddleware<RateLimitMiddleware>();
// If you want global CAPTCHA check, you can insert a CaptchaMiddleware too

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
