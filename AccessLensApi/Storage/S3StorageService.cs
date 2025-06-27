using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace AccessLensApi.Storage;

/// <summary>
/// Stores files in S3 under {bucket}/{key} and returns a presigned URL
/// valid for the requested TTL.
/// </summary>
public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3StorageService(IAmazonS3 s3, IConfiguration cfg)
    {
        _s3 = s3;
        _bucket = Environment.GetEnvironmentVariable("AWS_S3_BUCKET") ??
                 cfg["S3:BucketName"] ??
                 throw new ArgumentException("S3 bucket missing");
    }

    public async Task UploadAsync(string key, byte[] bytes, CancellationToken ct = default)
    {
        var put = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = new MemoryStream(bytes),
            ContentType = GuessContentType(key),
            CannedACL = S3CannedACL.Private
        };
        await _s3.PutObjectAsync(put, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var deleteRequest = new Amazon.S3.Model.DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = key
        };
        await _s3.DeleteObjectAsync(deleteRequest, ct);
    }

    public string GetPresignedUrl(string key, TimeSpan ttl)
    {
        if (ttl > TimeSpan.FromDays(7))
            ttl = TimeSpan.FromDays(7);

        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
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

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType switch
        {
            "application/pdf" => ".pdf",
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            _ => ""
        };
    }
}
