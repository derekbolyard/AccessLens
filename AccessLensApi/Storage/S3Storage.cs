using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace AccessLensApi.Storage;

/// <summary>
/// Stores files in S3 under {bucket}/{key} and returns a presigned URL
/// valid for the requested TTL.
/// </summary>
public sealed class S3Storage : IStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3Storage(IAmazonS3 s3, IConfiguration cfg)
    {
        _s3 = s3;
        _bucket = cfg["S3:Bucket"] ?? throw new ArgumentException("S3:Bucket missing");
    }

    public async Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default)
    {
        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = new MemoryStream(bytes),
            ContentType = GuessContentType(key),
            CannedACL = S3CannedACL.Private
        };
        await _s3.PutObjectAsync(put, ct);
    }

    public string GetUrl(string key, TimeSpan ttl)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = Protocol.HTTPS
        };
        return _s3.GetPreSignedURL(req);
    }

    private static string GuessContentType(string key)
        => Path.GetExtension(key).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
}
