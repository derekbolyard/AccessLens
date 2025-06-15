using AccessLensApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AccessLensApi.Features.Auth;

[ApiController]
[Route("api/auth")]
public class MagicLinkController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _cfg;

    public MagicLinkController(ApplicationDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    [HttpGet("magic/{token}")]
    public async Task<ActionResult> VerifyMagicLink(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = "accesslens",
            ValidAudience = "magic",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["MAGIC_JWT_SECRET"] ?? string.Empty)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireExpirationTime = true
        };

        ClaimsPrincipal jwt;
        try
        {
            jwt = handler.ValidateToken(token, validationParameters, out _);
        }
        catch (SecurityTokenException)
        {
            return Unauthorized("Invalid or expired magic link");
        }

        var jti = Guid.Parse(jwt.FindFirstValue(JwtRegisteredClaimNames.Jti)!);
        var email = jwt.FindFirstValue("email")!;

        // Check if token was already used
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existingUsage = await _db.EmailVerifications
                .AnyAsync(ev => ev.Code == jti.ToString());

            if (existingUsage)
                return BadRequest("Magic link already used");

            // Mark email as verified and user as authenticated
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.EmailVerified = true;
                // Record the magic link usage
                _db.EmailVerifications.Add(new Models.EmailVerification
                {
                    Email = email,
                    Code = jti.ToString(),
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(1) // Already used
                });
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var sessionToken = new MagicTokenService(_cfg).BuildSessionToken(email);

        // Redirect to frontend with SESSION token (not the short magic token)
        var frontendUrl = _cfg["Frontend:BaseUrl"] ?? "http://localhost:4200";
        return Redirect($"{frontendUrl}/auth/callback#token={sessionToken}");
    }
}