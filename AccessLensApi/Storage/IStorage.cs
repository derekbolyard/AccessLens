namespace AccessLensApi.Storage;

/// <summary>Abstraction so scanners / PDF code stay storage-agnostic.</summary>
public interface IStorage
{
    Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default);

    /// <param name="ttl">Time-to-live for the returned URL.</param>
    string GetUrl(string key, TimeSpan ttl);
}
