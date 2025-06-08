using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using AccessLensApi.Data;
using AccessLensApi.Models;
using AccessLensApi.Services.Interfaces;
using AccessLensApi.Utilities;
using AccessLensApi.Middleware;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessLensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScanController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ICreditManager _creditManager;
        private readonly IA11yScanner _scanner;
        private readonly IPdfService _pdf;
        private readonly ILogger<ScanController> _logger;
        private readonly IRateLimiter _rateLimiter;
        private readonly RateLimitingOptions _rateOptions;
        private readonly CaptchaOptions _captchaOptions;
        private readonly IHttpClientFactory _httpClientFactory;

        public ScanController(
            ApplicationDbContext dbContext,
            ICreditManager creditManager,
            IA11yScanner scanner,
            IPdfService pdf,
            ILogger<ScanController> logger,
            IRateLimiter rateLimiter,
            IOptions<RateLimitingOptions> rateOptions,
            IOptions<CaptchaOptions> captchaOptions,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _creditManager = creditManager;
            _scanner = scanner;
            _pdf = pdf;
            _logger = logger;
            _rateLimiter = rateLimiter;
            _rateOptions = rateOptions.Value;
            _captchaOptions = captchaOptions.Value;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// POST /api/scan/starter
        /// Body: { url, email }
        /// 
        /// 1) Validate URL and email.
        /// 2) Ensure User record exists.
        /// 3) If not verified & not firstScan: return needVerify.
        /// 4) If no quota: return needPayment.
        /// 5) Flip firstScan if this is the user’s first scan.
        /// 6) Run the a11y scanner (up to 5 pages).
        /// 7) Compute score from the first page’s results.
        /// 8) Generate and upload a PDF from the first page’s JSON.
        /// 9) Return { score, pdfUrl, teaserUrl }.
        /// </summary>
        [HttpPost("starter")]
        [RequestSizeLimit(1_048_576)]
        public async Task<IActionResult> Starter([FromBody] ScanRequest req)
        {
            var validationError = ValidateRequest(req);
            if (validationError != null)
                return validationError;

            if (string.IsNullOrEmpty(req.HcaptchaToken))
                return BadRequest(new { error = "hCaptcha token required." });

            if (!await VerifyHCaptchaAsync(req.HcaptchaToken))
                return BadRequest(new { error = "hCaptcha failed." });

            var email = req.Email.Trim().ToLowerInvariant();
            var url = req.Url.Trim();

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsUrlAllowed(uri))
                return BadRequest(new { error = "URL not allowed." });

            var user = await GetOrCreateUserAsync(email);

            // 3) If not verified & not firstScan: return needVerify
            var verifyError = CheckVerification(user);
            if (verifyError != null)
                return verifyError;

            // 4) If no quota: return needPayment
            var paymentError = await CheckQuotaAsync(email);
            if (paymentError != null)
                return paymentError;

            // 5) Flip firstScan if needed
            await UpdateFirstScanAsync(user);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!await _rateLimiter.TryAcquireStarterAsync(ip, email, user.EmailVerified))
                return StatusCode(429, new { error = "Rate limit exceeded" });

            JsonObject scanResult;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_rateOptions.MaxScanDurationSeconds));
            try
            {
                scanResult = await _scanner.ScanFivePagesAsync(url, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(500, new { error = "Scan timed out." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A11y scan failed for URL: {Url}", url);
                return StatusCode(500, new { error = "Accessibility scan failed." });
            }
            finally
            {
                _rateLimiter.ReleaseStarter();
            }

            // 7) Compute score from the first page’s results
            int score;
            try
            {
                score = ComputeScore(scanResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute score for URL: {Url}", url);
                return StatusCode(500, new { error = "Failed to compute accessibility score." });
            }

            // 8) Generate and upload PDF from the first page’s JSON
            string pdfUrl;
            try
            {
                pdfUrl = await GeneratePdfUrlAsync(url, scanResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF generation failed for URL: {Url}", url);
                return StatusCode(500, new { error = "PDF generation failed." });
            }

            var teaserUrl = ExtractTeaser(scanResult);

            return Ok(new { score, pdfUrl, teaserUrl });
        }

        // ---------- Private helper methods ----------

        private IActionResult? ValidateRequest(ScanRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.Url)
                || !Uri.IsWellFormedUriString(req.Url.Trim(), UriKind.Absolute))
            {
                return BadRequest(new { error = "Invalid URL." });
            }

            if (string.IsNullOrWhiteSpace(req.Email)
                || !new EmailAddressAttribute().IsValid(req.Email.Trim()))
            {
                return BadRequest(new { error = "Invalid email." });
            }

            return null;
        }

        private async Task<User> GetOrCreateUserAsync(string email)
        {
            var user = await _dbContext.Users.FindAsync(email);
            if (user != null)
                return user;

            user = new User
            {
                Email = email,
                EmailVerified = false,
                FirstScan = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private IActionResult? CheckVerification(User user)
        {
            if (!user.EmailVerified && !user.FirstScan)
            {
                return Ok(new { needVerify = true });
            }
            return null;
        }

        private async Task<IActionResult?> CheckQuotaAsync(string email)
        {
            var hasQuota = await _creditManager.HasQuotaAsync(email);
            if (!hasQuota)
                return Ok(new { needPayment = true });
            return null;
        }

        private async Task UpdateFirstScanAsync(User user)
        {
            if (!user.FirstScan)
                return;

            user.FirstScan = false;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }

        private int ComputeScore(JsonObject scanResult)
        {
            if (!scanResult.TryGetPropertyValue("pages", out var pagesNode)
                || pagesNode is not JsonArray pages
                || pages.Count == 0
                || pages[0] is not JsonObject firstPage)
            {
                throw new InvalidOperationException("Scan result did not contain any pages.");
            }

            return A11yScore.From(firstPage);
        }

        private async Task<string> GeneratePdfUrlAsync(string url, JsonObject scanResult)
        {
            var firstPage = ((JsonArray)scanResult["pages"]!)[0] as JsonObject;
            if (firstPage == null)
                throw new InvalidOperationException("First page JSON missing.");

            // Assume GenerateAndUploadPdf returns a public URL
            return await _pdf.GenerateAndUploadPdf(url, firstPage);
        }

        private string ExtractTeaser(JsonObject scanResult)
        {
            if (scanResult.TryGetPropertyValue("teaserUrl", out var teaserNode)
                && teaserNode is JsonValue teaserValue
                && teaserValue.TryGetValue<string>(out var teaserStr))
            {
                return teaserStr ?? "";
            }

            return "";
        }

        private bool IsUrlAllowed(Uri uri)
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
            {
                var ip = System.Net.IPAddress.Parse(uri.Host);
                if (System.Net.IPAddress.IsLoopback(ip)) return false;

                var bytes = ip.GetAddressBytes();
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    if (bytes[0] == 10 ||
                        (bytes[0] == 192 && bytes[1] == 168) ||
                        (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31))
                        return false;
                }
                if (ip.IsIPv6LinkLocal) return false;
            }

            return true;
        }

        private async Task<bool> VerifyHCaptchaAsync(string token)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var values = new Dictionary<string, string>
                {
                    { "secret", _captchaOptions.hCaptchaSecret },
                    { "response", token }
                };
                using var content = new FormUrlEncodedContent(values);
                using var resp = await client.PostAsync("https://hcaptcha.com/siteverify", content);
                if (!resp.IsSuccessStatusCode)
                    return false;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "hCaptcha verification failed");
                return false;
            }
        }
    }
}
