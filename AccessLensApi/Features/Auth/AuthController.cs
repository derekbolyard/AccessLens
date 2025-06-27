using AccessLensApi.Data;
using AccessLensApi.Features.Auth.Models;
using AccessLensApi.Features.Core.Interfaces;
using AccessLensApi.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AccessLensApi.Features.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IMagicTokenService _magicTokenService;
        private readonly IConfiguration _cfg;
        private readonly IAntiforgery _antiForgery;

        public AuthController(
            ApplicationDbContext dbContext,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IMagicTokenService magicTokenService,
            IConfiguration cfg,
            IAntiforgery antiForgery)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;
            _magicTokenService = magicTokenService;
            _cfg = cfg;
            _antiForgery = antiForgery;
        }

        /// <summary>
        /// POST /api/auth/send-magic-link
        /// Body: { email }
        /// Generates a JWT magic link and emails it to the user.
        /// </summary>
        [HttpPost("send-magic-link")]
        public async Task<IActionResult> SendMagicLink([FromBody] SendMagicLinkRequest body)
        {
            if (string.IsNullOrWhiteSpace(body?.Email))
                return BadRequest(new { error = "Invalid email." });

            try
            {
                // Ensure user exists
                var user = await _dbContext.Users.FindAsync(body.Email);
                if (user == null)
                {
                    user = new User { Email = body.Email };
                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();
                }

                // Generate magic link token
                var magicToken = _magicTokenService.BuildMagicToken(body.Email);

                // Send magic link email
                await _emailService.SendMagicLinkAsync(body.Email, magicToken);

                return Ok(new { message = "Magic link sent to your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send magic link to {Email}", body.Email);
                return StatusCode(500, new { error = "Failed to send magic link. Please try again." });
            }
        }

        [HttpGet("magic/{token}")]
        public async Task<ActionResult> VerifyMagicLink(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var parms = new TokenValidationParameters
            {
                ValidIssuer = "accesslens",
                ValidAudience = "magic",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_cfg["Jwt:SecretKey"]!)),
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            ClaimsPrincipal jwt;
            try { jwt = handler.ValidateToken(token, parms, out _); }
            catch (SecurityTokenException) { return Unauthorized("Invalid or expired link"); }

            var jti = jwt.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
            var email = jwt.FindFirstValue(ClaimTypes.Email);

            // Check if token was already used
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existingUsage = await _dbContext.MagicLinkUsages
                    .AnyAsync(mlu => mlu.JwtId == jti.ToString());

                if (existingUsage)
                    return BadRequest("Magic link already used");

                // Mark email as verified and user as authenticated
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    user.EmailVerified = true;

                    // Record the magic link usage
                    _dbContext.MagicLinkUsages.Add(new MagicLinkUsage
                    {
                        Email = email,
                        JwtId = jti.ToString(),
                        UsedAt = DateTime.UtcNow,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(1) // Mark as used
                    });
                    await _dbContext.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            var sessionJwt = _magicTokenService.BuildSessionToken(email);

            // 4️⃣  Drop it as HttpOnly cookie
            var cookieDomain = ".accesslens.app";

            Response.Cookies.Append("access_token", sessionJwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Domain = cookieDomain,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            // 5️⃣  Seed CSRF token (Angular will read JS-visible cookie + echo header)
            var tokens = _antiForgery.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Domain = cookieDomain,
                // not HttpOnly so Angular can read & send it
            });

            // 6️⃣  Redirect to clean SPA root—no token/hash fragment needed
            var frontend = _cfg["Urls:WebAppUrl"] ?? "http://localhost:4200";
            return Redirect($"{frontend}/");
        }
    }
}
