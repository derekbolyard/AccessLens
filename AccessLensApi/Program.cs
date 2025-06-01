using AccessLensApi.Services;
using AccessLensApi.Storage;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"          // ← set here, not later
});

QuestPDF.Settings.License = LicenseType.Community;

builder.Services
    // 1) bind options from config
    .Configure<PlaywrightOptions>(builder.Configuration.GetSection("Playwright"))

    // 2) singleton IPlaywright (one per process)
    .AddSingleton<IPlaywright>(sp =>
    {
        // CreateAsync is still async; use GetAwaiter to block once at startup
        return Playwright.CreateAsync().GetAwaiter().GetResult();
    })

    // 3) singleton IBrowser built from options
    .AddSingleton<IBrowser>(sp =>
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

#if DEBUG
builder.Services.AddSingleton<IStorage, LocalStorage>();
#else
builder.Services.AddAWSService<IAmazonS3>();          // uses default AWS creds/region
builder.Services.AddSingleton<IStorage, S3Storage>();
#endif

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
