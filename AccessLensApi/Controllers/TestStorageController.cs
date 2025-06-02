using AccessLensApi.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AccessLensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestStorageController : ControllerBase
    {
        private readonly IStorageService _storageService;

        public TestStorageController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// GET /api/TestStorage/test
        /// 
        /// 1) Uploads a small text blob ("Hello from S3!") as a new object with a GUID key.
        /// 2) Generates a presigned URL valid for 30 minutes.
        /// 3) Returns { key, url } in JSON.
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestStorageAsync()
        {
            // 1) Prepare some dummy data
            var content = "Hello from S3!";
            var data = Encoding.UTF8.GetBytes(content);
            var objectKey = $"test-{Guid.NewGuid()}.txt";

            try
            {
                // 2) Upload the byte[] to S3 (cancellation token is None for simplicity)
                await _storageService.UploadAsync(objectKey, data, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
            }

            string presignedUrl;
            try
            {
                // 3) Generate a presigned GET URL valid for 30 minutes
                presignedUrl = _storageService.GetPresignedUrl(objectKey, TimeSpan.FromMinutes(30));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Presigned URL generation failed: {ex.Message}" });
            }

            // 4) Return the key and URL
            return Ok(new
            {
                key = objectKey,
                url = presignedUrl
            });
        }
    }
}
