using AccessLensApi.Features.Reports;
using AccessLensApi.Storage;
using GenerateSample;
using System.Reflection.Metadata.Ecma335;

var outDir = args.Length > 1 && args[0] == "--output"
             ? args[1]
             : "./sample-output";
Directory.CreateDirectory(outDir);

// ➊ Build realistic demo data
var report = SampleData.DemoReport();
report.PopulateFromScanResult();

// ➋ Render & save
var storage = new LocalFolderStorage(outDir);
var builder = new ReportBuilder(storage);

var html = builder.RenderHtml(report);
await File.WriteAllTextAsync(Path.Combine(outDir, "report-sample.html"), html);
await builder.GeneratePdfAsync(html, "report-sample.pdf");

Console.WriteLine($"✔  Sample written to {outDir}");

// ───────────────────────────────────────────────────────────────────────────────
// LocalFolderStorage simply dumps bytes to disk; satisfies IStorageService.
sealed class LocalFolderStorage : IStorageService
{
    private readonly string _root;
    public LocalFolderStorage(string root) => _root = root;

    public async Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default)
    {
        // convert S3-style "reports/file.pdf" → local path
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes, ct);
    }

    public string GetPresignedUrl(string key, TimeSpan ttl) =>
        string.Empty;

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
