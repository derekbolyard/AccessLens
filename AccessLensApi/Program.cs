using AccessLensApi.Config;
using AccessLensApi.Data;
using AccessLensApi.Extensions;
using AccessLensApi.Features.Auth;
using AccessLensApi.Features.Reports;
using AccessLensApi.Features.Core.Interfaces;
using AccessLensApi.Features.Core.Services;
using AccessLensApi.Features.Scans.Services;
using AccessLensApi.Features.Payments.Services;
using AccessLensApi.Middleware;
using AccessLensApi.Storage;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Events;
using Stripe.Events;
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
// 1️⃣  CONFIG + LOGGING + SERVICES
// ------------------------------------------------------------------
builder.Services
    .AddConfigurationOptions(builder.Configuration)
    .AddSecurityServices()
    .AddCustomAuthentication(builder.Configuration)
    .AddDataAccess(builder.Configuration.GetConnectionString("Sqlite")!)
    .AddExternalServices(builder.Configuration)
    .AddBusinessServices();

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .MinimumLevel.Information()
        .WriteTo.Console());

// ------------------------------------------------------------------
// 2️⃣  HTTP CLIENT + CACHE + MVC/SWAGGER + CORS + SESSION
// ------------------------------------------------------------------
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomCors(builder.Environment);



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

app.ConfigurePipeline()
   .ConfigureEndpoints();

app.Run();

public partial class Program { }