using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AccessLensApi.Features.Auth
{
    public class MagicTokenService : IMagicTokenService
    {
        private readonly string _key;

        public MagicTokenService(IConfiguration cfg)
        {
            _key = Environment.GetEnvironmentVariable("MAGIC_JWT_SECRET") ??
                cfg["MagicJwt:SecretKey"] ??
                throw new InvalidOperationException("MAGIC_JWT_SECRET is required");
            if (_key.Length < 32)
                throw new InvalidOperationException("MAGIC_JWT_SECRET must be at least 32 characters");
        }

        // Short-lived magic link token (15 min)
        public string BuildMagicToken(string email)
        {
            var now = DateTimeOffset.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: "accesslens",
                audience: "magic",
                claims: new[]
                {
                new Claim("email", email),
                new Claim("type", "magic"), // Mark as magic link token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                },
                notBefore: now.UtcDateTime,
                expires: now.AddMinutes(15).UtcDateTime, // 15 minutes for magic link
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                    SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        // Long-lived session token (24 hours)
        public string BuildSessionToken(string email)
        {
            var now = DateTimeOffset.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: "accesslens",
                audience: "api",
                claims: new[]
                {
                new Claim("email", email),
                new Claim("type", "session"), // Mark as session token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                },
                notBefore: now.UtcDateTime,
                expires: now.AddHours(24).UtcDateTime, // 24 hours for API access
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                    SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
