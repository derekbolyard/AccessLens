using AccessLensApi.Data;
using AccessLensApi.Features.Auth.Models;
using AccessLensApi.Middleware;
using AccessLensApi.Models;
using AccessLensApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        public AuthController(
            ApplicationDbContext dbContext,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IMagicTokenService magicTokenService)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;
            _magicTokenService = magicTokenService;
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
    }
}
