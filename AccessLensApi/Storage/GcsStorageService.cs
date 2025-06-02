using AccessLensApi.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;                   // for StorageService.Scope.DevstorageFullControl
using Google.Cloud.Storage.V1;

namespace AccessLensApi.Services.Storage
{
    public class GcsStorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly UrlSigner _urlSigner;
        private readonly string _bucketName;

        public GcsStorageService(IConfiguration config)
        {
            // 1) Read the bucket name from configuration
            _bucketName = config["Gcs:BucketName"]
                          ?? throw new ArgumentNullException("Gcs:BucketName must be set");

            // 2) Read the entire service account JSON from config or environment
            var saJson = config["Gcs:ServiceAccountJson"]
                         ?? Environment.GetEnvironmentVariable("GCS_SERVICE_ACCOUNT_JSON");

            if (string.IsNullOrEmpty(saJson))
            {
                throw new ArgumentNullException(
                    "Gcs:ServiceAccountJson (or env GCS_SERVICE_ACCOUNT_JSON) must be set to the service account JSON"
                );
            }

            // 3) Create a GoogleCredential from that JSON, scoped for full control of Cloud Storage
            GoogleCredential googleCred;
            try
            {
                googleCred = GoogleCredential.FromJson(saJson)
                                             .CreateScoped(StorageService.Scope.DevstorageFullControl);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to parse or scope the GCS service account JSON.", ex
                );
            }

            // 4) Create the StorageClient using those credentials
            _storageClient = StorageClient.Create(googleCred);

            // 5) Extract the underlying ServiceAccountCredential
            if (!(googleCred.UnderlyingCredential is ServiceAccountCredential svcCred))
            {
                throw new InvalidOperationException(
                    "The provided GoogleCredential is not a ServiceAccountCredential. " +
                    "Ensure you supplied valid service-account JSON."
                );
            }

            // 6) Create a UrlSigner from the ServiceAccountCredential (for signed URLs)
            _urlSigner = UrlSigner.FromCredential(svcCred);
        }

        /// <summary>
        /// Uploads raw bytes to the specified objectName in the bucket.
        /// </summary>
        /// <param name="objectName">The object key (e.g. "abc123.pdf").</param>
        /// <param name="data">The byte[] content to upload.</param>
        /// <param name="cancellationToken">Cancellation token for the upload.</param>
        public async Task UploadAsync(string objectName, byte[] data, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(data);
            var obj = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = _bucketName,
                Name = objectName,
                ContentType = "application/octet-stream"
            };
            await _storageClient.UploadObjectAsync(
                obj,
                stream,
                new UploadObjectOptions(),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Generates a V4 signed GET URL for the specified object, valid for <paramref name="expiration"/>.
        /// </summary>
        /// <param name="objectName">The object key (e.g. "abc123.pdf").</param>
        /// <param name="expiration">How long the signed URL should remain valid.</param>
        /// <returns>A time-limited HTTPS URL that allows anyone to GET the object.</returns>
        public string GetPresignedUrl(string objectName, TimeSpan expiration)
        {
            return _urlSigner.Sign(
                _bucketName,
                objectName,
                expiration,
                HttpMethod.Get
            );
        }
    }
}
