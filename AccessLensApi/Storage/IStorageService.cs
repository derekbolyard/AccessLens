namespace AccessLensApi.Storage;

/// <summary>Abstraction so scanners / PDF code stay storage-agnostic.</summary>
public interface IStorageService
{
    Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <param name="ttl">Time-to-live for the returned URL.</param>
    string GetPresignedUrl(string key, TimeSpan ttl);
}

