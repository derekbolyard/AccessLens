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
        // fallback when WebRootPath is null (minimal APIs)
        var webRoot = env.WebRootPath ??
                      Path.Combine(env.ContentRootPath, "wwwroot");

        _basePath = Path.Combine(webRoot, "teasers");
        Directory.CreateDirectory(_basePath);

        _baseUrl = cfg["BaseUrl"] ?? "https://localhost:7088";
    }

    public async Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, Path.GetFileName(key));
        await File.WriteAllBytesAsync(filePath, bytes, ct);
    }

    public string GetPresignedUrl(string key, TimeSpan ttl)
        => $"{_baseUrl.TrimEnd('/')}/teasers/{Path.GetFileName(key)}";
}
