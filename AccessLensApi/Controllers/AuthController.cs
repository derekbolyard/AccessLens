using AccessLensApi.Data;
using AccessLensApi.Middleware;
using AccessLensApi.Models;
using AccessLensApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AccessLensApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly CaptchaOptions _captchaOptions;

        public AuthController(
            ApplicationDbContext dbContext,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IOptions<CaptchaOptions> captchaOptions)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;
            _captchaOptions = captchaOptions.Value;
        }

        /// <summary>
        /// POST /api/auth/send-code
        /// Body: { email }
        /// Generates or reuses a 6-digit code (if not expired) and emails it.
        /// </summary>
        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] SendCodeRequest body)
        {
            if (string.IsNullOrWhiteSpace(body?.Email))
                return BadRequest(new { error = "Invalid email." });

            // Ensure user exists
            var user = await _dbContext.Users.FindAsync(body.Email);
            if (user == null)
            {
                user = new User { Email = body.Email };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            // Check if there’s an existing code that hasn’t expired
            var now = DateTime.UtcNow;

            // Generate new 6-digit code
            var code = new Random().Next(100000, 999999).ToString();
            var expires = now.AddMinutes(15);

            // Upsert
            var evEntity = new EmailVerification
            {
                Email = body.Email,
                Code = code,
                ExpiresUtc = expires
            };
            _dbContext.EmailVerifications.Add(evEntity);
            await _dbContext.SaveChangesAsync();

            await _emailService.SendVerificationCodeAsync(body.Email, code);

            return Ok(new { message = "Verification code sent." });
        }

        /// <summary>
        /// POST /api/auth/verify
        /// Body: { email, code, hcaptchaToken? }
        /// Verifies the 6-digit code; if correct, sets EmailVerified = true.
        /// If 3 invalid attempts have occurred, requires hCaptcha.
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("email", out var emailProp) || !body.TryGetProperty("code", out var codeProp))
                return BadRequest(new { error = "Missing email or code." });

            var email = emailProp.GetString().Trim().ToLowerInvariant();
            var code = codeProp.GetString().Trim();

            var now = DateTime.UtcNow;

            // Check stored code
            var ev = await _dbContext.EmailVerifications.FindAsync(email);
            if (ev == null || ev.ExpiresUtc < now)
            {
                return BadRequest(new { error = "Code expired or not found." });
            }

            // If the user has too many invalid attempts, require hCaptcha
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var failKey = $"VerifyFails-{ip}-{email}";
            var failCount = HttpContext.Session.GetInt32(failKey) ?? 0;

            if (failCount >= 3)
            {
                if (!body.TryGetProperty("hcaptchaToken", out var tokenProp))
                {
                    return BadRequest(new { error = "hCaptcha required." });
                }

                var token = tokenProp.GetString();
                var validCaptcha = await VerifyHCaptchaAsync(token);
                if (!validCaptcha)
                {
                    return BadRequest(new { error = "hCaptcha failed." });
                }
            }

            if (ev.Code != code)
            {
                HttpContext.Session.SetInt32(failKey, failCount + 1);
                return BadRequest(new { error = "Invalid code." });
            }

            // Correct code: mark verified, delete verification row
            var user = await _dbContext.Users.FindAsync(email);
            if (user != null)
            {
                user.EmailVerified = true;
                _dbContext.Users.Update(user);
                _dbContext.EmailVerifications.Remove(ev);
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { message = "Email verified." });
        }

        private async Task<bool> VerifyHCaptchaAsync(string token)
        {
            using var client = new HttpClient();
            var secret = _captchaOptions.hCaptchaSecret;
            var values = new Dictionary<string, string>
            {
                { "secret", secret },
                { "response", token }
            };
            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://hcaptcha.com/siteverify", content);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("success", out var successProp)
                && successProp.GetBoolean())
            {
                return true;
            }
            return false;
        }
    }
}
