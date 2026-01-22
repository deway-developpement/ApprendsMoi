using System.Text;

namespace backend.Helpers;

public static class EnvironmentValidator {
    public static void ValidateRequiredVariables() {
        var errors = new List<string>();

        // JWT Secret
        var jwtSecret = Environment.GetEnvironmentVariable("JWT__SECRET");
        if (string.IsNullOrEmpty(jwtSecret)) {
            errors.Add("JWT__SECRET is not configured");
        } else if (Encoding.UTF8.GetByteCount(jwtSecret) < 32) {
            errors.Add("JWT__SECRET must be at least 32 bytes (256 bits) long for HMAC-SHA256");
        }

        // Backend URL
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BACKEND__URL"))) {
            errors.Add("BACKEND__URL is not configured");
        }

        // Frontend URL
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FRONTEND__URL"))) {
            errors.Add("FRONTEND__URL is not configured");
        }

        // PostgreSQL Connection String
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES__CONNECTION_STRING"))) {
            errors.Add("POSTGRES__CONNECTION_STRING is not configured");
        }

        // JWT Access Token Expiry
        var accessExpiryEnv = Environment.GetEnvironmentVariable("JWT__ACCESS_EXPIRES_IN_HOURS");
        if (string.IsNullOrEmpty(accessExpiryEnv)) {
            errors.Add("JWT__ACCESS_EXPIRES_IN_HOURS is not configured");
        } else if (!double.TryParse(accessExpiryEnv, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var accessExpiry) || accessExpiry <= 0) {
            errors.Add($"JWT__ACCESS_EXPIRES_IN_HOURS has invalid value: '{accessExpiryEnv}'. Must be a positive number");
        }

        // JWT Refresh Token Expiry
        var refreshExpiryEnv = Environment.GetEnvironmentVariable("JWT__REFRESH_EXPIRES_IN_HOURS");
        if (string.IsNullOrEmpty(refreshExpiryEnv)) {
            errors.Add("JWT__REFRESH_EXPIRES_IN_HOURS is not configured");
        } else if (!double.TryParse(refreshExpiryEnv, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var refreshExpiry) || refreshExpiry <= 0) {
            errors.Add($"JWT__REFRESH_EXPIRES_IN_HOURS has invalid value: '{refreshExpiryEnv}'. Must be a positive number");
        }

        // Stops process if errors
        if (errors.Count > 0) {
            var errorMessage = "Environment validation failed:\n" + string.Join("\n", errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(errorMessage);
        }
    }
}
