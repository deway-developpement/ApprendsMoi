using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace backend.Domains.Zoom;

[ApiController]
[Route("api/zoom")]
public class ZoomController : ControllerBase
{
    private readonly string? _sdkKey;
    private readonly string? _sdkSecret;

    public ZoomController(IConfiguration config)
    {
        _sdkKey = Environment.GetEnvironmentVariable("ZOOM_SDK_KEY") ?? config["Zoom:SdkKey"];
        _sdkSecret = Environment.GetEnvironmentVariable("ZOOM_SDK_SECRET") ?? config["Zoom:SdkSecret"];
    }

    [HttpPost("signature")]
    public IActionResult GenerateSignature([FromBody] ZoomSignatureRequest request)
    {
        if (string.IsNullOrWhiteSpace(_sdkKey) || string.IsNullOrWhiteSpace(_sdkSecret))
        {
            return StatusCode(500, "ZOOM_SDK_KEY / ZOOM_SDK_SECRET manquants dans l'environnement ou la configuration.");
        }

        if (string.IsNullOrWhiteSpace(request.MeetingNumber))
        {
            return BadRequest("meetingNumber requis");
        }

        try
        {
            var signature = CreateZoomSignature(_sdkKey, _sdkSecret, request.MeetingNumber, request.Role);
            return Ok(new { signature, sdkKey = _sdkKey });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la génération de la signature Zoom: {ex.Message}");
        }
    }

    private static string CreateZoomSignature(string sdkKey, string sdkSecret, string meetingNumber, int role)
    {
        // Based on Zoom docs: header + payload signed with SDK secret (HS256), base64url encoded.
        var ts = ToUnixTimeSeconds(DateTime.UtcNow) - 30; // iat slightly backdated
        var exp = ts + 60 * 60 * 2; // 2h validity

        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            sdkKey,
            mn = meetingNumber,
            role,
            iat = ts,
            exp,
            appKey = sdkKey,
            tokenExp = exp
        };

        string headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(header)));
        string payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload)));
        string message = $"{headerBase64}.{payloadBase64}";

        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(sdkSecret));
        var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(message));
        string signature = Base64UrlEncode(hash);

        return $"{message}.{signature}";
    }

    private static long ToUnixTimeSeconds(DateTime dateTime) =>
        (long)Math.Floor((dateTime - DateTime.UnixEpoch).TotalSeconds);

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public class ZoomSignatureRequest
{
    public string MeetingNumber { get; set; } = string.Empty;
    public int Role { get; set; } = 0; // 0 = participant, 1 = host
}
