using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AccessLensApi.Storage;

/// <summary>
/// Writes files to wwwroot/teasers/ and returns absolute URLs like
/// https://localhost:7048/teasers/{file}.png
/// </summary>
public sealed class LocalStorage : IStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;  // e.g. https://localhost:7088

    public LocalStorage(IWebHostEnvironment env, IConfiguration cfg)
    {
        // Determine where files should be written. When running on Fly.io this
        // can point at a mounted volume.
        var root = Environment.GetEnvironmentVariable("LOCAL_STORAGE_ROOT");
        if (string.IsNullOrEmpty(root))
        {
            // fallback when WebRootPath is null (minimal APIs)
            var webRoot = env.WebRootPath ??
                          Path.Combine(env.ContentRootPath, "wwwroot");
            root = webRoot;
        }

        _basePath = Path.Combine(root, "teasers");
        Directory.CreateDirectory(_basePath);

        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ??
                   cfg["BaseUrl"] ??
                   "https://localhost:7088";
    }

    public async Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, Path.GetFileName(key));
        await File.WriteAllBytesAsync(filePath, bytes, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, Path.GetFileName(key));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public string GetPresignedUrl(string key, TimeSpan ttl)
        => $"{_baseUrl.TrimEnd('/')}/teasers/{Path.GetFileName(key)}";
}
