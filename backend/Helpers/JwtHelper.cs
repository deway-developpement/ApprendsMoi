using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Database.Models;
using Microsoft.IdentityModel.Tokens;

namespace backend.Helpers;

public class JwtHelper {
    public static string GenerateToken(Guid userId, string? email, string? username, ProfileType profile) {
        var jwtSecret = Environment.GetEnvironmentVariable("JWT__SECRET");
        if (string.IsNullOrEmpty(jwtSecret)) {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, profile.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        if (!string.IsNullOrEmpty(email)) {
            claimsList.Add(new Claim(ClaimTypes.Email, email));
        }
        if (!string.IsNullOrEmpty(username)) {
            claimsList.Add(new Claim(ClaimTypes.Name, username));
        }

        var claims = claimsList.ToArray();

        var expiresInHours = double.Parse(
            Environment.GetEnvironmentVariable("JWT__ACCESS_EXPIRES_IN_HOURS")!,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture
        );

        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("BACKEND__URL"),
            audience: Environment.GetEnvironmentVariable("FRONTEND__URL"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresInHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static Guid? GetUserIdFromClaims(ClaimsPrincipal user) {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)) {
            return userId;
        }
        return null;
    }

    public static string? GetUserEmailFromClaims(ClaimsPrincipal user) {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static ProfileType? GetUserProfileFromClaims(ClaimsPrincipal user) {
        var profileClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(profileClaim)) return null;
        
        return Enum.TryParse<ProfileType>(profileClaim, out var profile) ? profile : null;
    }

    public static string GenerateRefreshToken() {
        var bytes = Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray();
        return Convert.ToBase64String(bytes);
    }

    public static (string Token, DateTime Expiry) GenerateRefreshTokenWithExpiry() {
        var refreshToken = GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddHours(GetRefreshTokenExpiryHours());
        return (refreshToken, expiry);
    }

    public static double GetRefreshTokenExpiryHours() {
        var refreshExpiresInHoursEnv = Environment.GetEnvironmentVariable("JWT__REFRESH_EXPIRES_IN_HOURS");
        
        if (string.IsNullOrEmpty(refreshExpiresInHoursEnv)) {
            throw new InvalidOperationException("JWT__REFRESH_EXPIRES_IN_HOURS environment variable is not configured.");
        }
        
        if (!double.TryParse(refreshExpiresInHoursEnv, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedRefreshExpiresInHours)) {
            throw new InvalidOperationException($"JWT__REFRESH_EXPIRES_IN_HOURS has invalid value: '{refreshExpiresInHoursEnv}'. Must be a valid number.");
        }
        
        if (parsedRefreshExpiresInHours <= 0) {
            throw new InvalidOperationException($"JWT__REFRESH_EXPIRES_IN_HOURS must be greater than 0. Got: {parsedRefreshExpiresInHours}");
        }
        
        return parsedRefreshExpiresInHours;
    }

    public static bool HasPasswordComplexity(string password) {
        if (password.Length < 6) return false;
        
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        
        return hasUpper && hasLower && hasDigit;
    }
}
