using AccessLensApi.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace AccessLensApi.Common.Services
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> UploadImageAsync(IFormFile file, string folder, FileUploadOptions? options = null);
        Task<bool> DeleteFileAsync(string key);
        string GetFileUrl(string key, TimeSpan? expiry = null);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IStorageService _storage;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IStorageService storage, ILogger<FileUploadService> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string folder, FileUploadOptions? options = null)
        {
            options ??= new FileUploadOptions();

            try
            {
                // Validate file
                var validationResult = ValidateImageFile(file, options);
                if (!validationResult.IsValid)
                {
                    return FileUploadResult.Failure(validationResult.ErrorMessage!);
                }

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var key = $"{folder.Trim('/')}/{fileName}";

                // Process image if needed
                using var stream = file.OpenReadStream();
                byte[] imageData;

                if (options.ResizeMaxWidth.HasValue || options.ResizeMaxHeight.HasValue)
                {
                    imageData = await ResizeImageAsync(stream, options.ResizeMaxWidth, options.ResizeMaxHeight, options.Quality);
                }
                else
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                // Upload to storage
                await _storage.UploadAsync(key, imageData);

                return FileUploadResult.Success(key, _storage.GetPresignedUrl(key, TimeSpan.FromDays(365)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image");
                return FileUploadResult.Failure("Failed to upload image");
            }
        }

        public async Task<bool> DeleteFileAsync(string key)
        {
            try
            {
                await _storage.DeleteAsync(key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {Key}", key);
                return false;
            }
        }

        public string GetFileUrl(string key, TimeSpan? expiry = null)
        {
            return _storage.GetPresignedUrl(key, expiry ?? TimeSpan.FromDays(1));
        }

        private static FileValidationResult ValidateImageFile(IFormFile file, FileUploadOptions options)
        {
            if (file.Length == 0)
                return new FileValidationResult(false, "File is empty");

            if (file.Length > options.MaxFileSizeBytes)
                return new FileValidationResult(false, $"File size exceeds maximum allowed size of {options.MaxFileSizeBytes / (1024 * 1024)}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!options.AllowedExtensions.Contains(extension))
                return new FileValidationResult(false, $"File type not allowed. Allowed types: {string.Join(", ", options.AllowedExtensions)}");

            return new FileValidationResult(true);
        }

        private static async Task<byte[]> ResizeImageAsync(Stream inputStream, int? maxWidth, int? maxHeight, int quality)
        {
            using var image = await Image.LoadAsync(inputStream);
            
            if (maxWidth.HasValue || maxHeight.HasValue)
            {
                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth ?? image.Width, maxHeight ?? image.Height)
                };

                image.Mutate(x => x.Resize(resizeOptions));
            }

            using var outputStream = new MemoryStream();
            IImageEncoder encoder = image.Metadata.DecodedImageFormat?.DefaultMimeType switch
            {
                "image/jpeg" => new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = quality },
                "image/png" => new SixLabors.ImageSharp.Formats.Png.PngEncoder(),
                _ => new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = quality }
            };

            await image.SaveAsync(outputStream, encoder);
            return outputStream.ToArray();
        }
    }

    public class FileUploadOptions
    {
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
        public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif" };
        public int? ResizeMaxWidth { get; set; }
        public int? ResizeMaxHeight { get; set; }
        public int Quality { get; set; } = 85;
    }

    public class FileUploadResult
    {
        public bool IsSuccess { get; set; }
        public string? Key { get; set; }
        public string? Url { get; set; }
        public string? ErrorMessage { get; set; }

        public static FileUploadResult Success(string key, string url) =>
            new() { IsSuccess = true, Key = key, Url = url };

        public static FileUploadResult Failure(string errorMessage) =>
            new() { IsSuccess = false, ErrorMessage = errorMessage };
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public FileValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
}
