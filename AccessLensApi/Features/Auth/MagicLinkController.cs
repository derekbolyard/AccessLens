using AccessLensApi.Data;
using AccessLensApi.Features.Auth.Models;
using Microsoft.AspNetCore.Antiforgery;
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
    private readonly IMagicTokenService _magicTokenService;
    private readonly IAntiforgery _antiForgery;

    public MagicLinkController(ApplicationDbContext db, IConfiguration cfg, IMagicTokenService magicTokenService, IAntiforgery antiForgery)
    {
        _db = db;
        _cfg = cfg;
        _magicTokenService = magicTokenService;
        _antiForgery = antiForgery;
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
                Encoding.UTF8.GetBytes(_cfg["MagicJwt:SecretKey"]!)),
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
        var email = jwt.FindFirstValue("email")!;

        // Check if token was already used
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existingUsage = await _db.MagicLinkUsages
                .AnyAsync(mlu => mlu.JwtId == jti.ToString());

            if (existingUsage)
                return BadRequest("Magic link already used");

            // Mark email as verified and user as authenticated
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.EmailVerified = true;

                // Record the magic link usage
                _db.MagicLinkUsages.Add(new MagicLinkUsage
                {
                    Email = email,
                    JwtId = jti.ToString(),
                    UsedAt = DateTime.UtcNow,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(1) // Mark as used
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