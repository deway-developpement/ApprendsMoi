using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace backend.Domains.Zoom;

public class ZoomSignatureService
{
    private readonly string? _sdkKey;
    private readonly string? _sdkSecret;

    public ZoomSignatureService(IConfiguration config)
    {
        _sdkKey = Environment.GetEnvironmentVariable("ZOOM_SDK_KEY") ?? config["Zoom:SdkKey"];
        _sdkSecret = Environment.GetEnvironmentVariable("ZOOM_SDK_SECRET") ?? config["Zoom:SdkSecret"];
    }

    public string GenerateSignature(string meetingNumber, int role = 0)
    {
        if (string.IsNullOrWhiteSpace(_sdkKey) || string.IsNullOrWhiteSpace(_sdkSecret))
        {
            throw new InvalidOperationException("Missing SDK Key/Secret");
        }

        var ts = ToUnixTimeSeconds(DateTime.UtcNow) - 30;
        var exp = ts + 60 * 60 * 2; // 2h validity

        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            sdkKey = _sdkKey,
            mn = meetingNumber,
            role,
            iat = ts,
            exp,
            appKey = _sdkKey,
            tokenExp = exp
        };

        string headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        string payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        string message = $"{headerBase64}.{payloadBase64}";

        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(_sdkSecret));
        var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(message));
        string signature = Base64UrlEncode(hash);

        return $"{message}.{signature}";
    }

    public string GetSdkKey()
    {
        if (string.IsNullOrWhiteSpace(_sdkKey))
        {
            throw new InvalidOperationException("Missing SDK Key");
        }
        return _sdkKey;
    }

    private static long ToUnixTimeSeconds(DateTime dateTime) =>
        (long)Math.Floor((dateTime - DateTime.UnixEpoch).TotalSeconds);

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
