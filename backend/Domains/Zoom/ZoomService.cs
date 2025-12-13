using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Domains.Zoom;

public class ZoomService
{
    private readonly string? _accountId;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string? _sdkKey;
    private readonly string? _sdkSecret;
    private readonly HttpClient _httpClient;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ZoomService(IConfiguration config, HttpClient httpClient)
    {
        _accountId = Environment.GetEnvironmentVariable("ZOOM_ACCOUNT_ID") ?? config["Zoom:AccountId"];
        _clientId = Environment.GetEnvironmentVariable("ZOOM_CLIENT_ID") ?? config["Zoom:ClientId"];
        _clientSecret = Environment.GetEnvironmentVariable("ZOOM_CLIENT_SECRET") ?? config["Zoom:ClientSecret"];
        _sdkKey = Environment.GetEnvironmentVariable("ZOOM_SDK_KEY") ?? config["Zoom:SdkKey"];
        _sdkSecret = Environment.GetEnvironmentVariable("ZOOM_SDK_SECRET") ?? config["Zoom:SdkSecret"];
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.zoom.us/v2/");
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedAccessToken;
        }

        if (string.IsNullOrWhiteSpace(_accountId) || string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
        {
            throw new InvalidOperationException("Configuration Zoom manquante (Account ID, Client ID, Client Secret)");
        }

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={_accountId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<ZoomTokenResponse>(content);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Impossible d'obtenir le token d'accès Zoom");
        }

        _cachedAccessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5 min buffer

        return _cachedAccessToken;
    }

    public async Task<ZoomMeeting> CreateInstantMeetingAsync(string topic = "ApprendsMoi - Session")
    {
        var token = await GetAccessTokenAsync();

        var meetingData = new
        {
            topic,
            type = 1, // Instant meeting
            settings = new
            {
                join_before_host = true,
                waiting_room = false,
                mute_upon_entry = false,
                approval_type = 2, // No registration required
                auto_recording = "none"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "users/me/meetings")
        {
            Content = new StringContent(JsonSerializer.Serialize(meetingData), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Erreur lors de la création de la réunion Zoom: {error}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var meeting = JsonSerializer.Deserialize<ZoomMeeting>(content);

        if (meeting == null)
        {
            throw new InvalidOperationException("Impossible de parser la réponse de création de meeting");
        }

        return meeting;
    }

    public string GenerateSignature(string meetingNumber, int role = 0)
    {
        if (string.IsNullOrWhiteSpace(_sdkKey) || string.IsNullOrWhiteSpace(_sdkSecret))
        {
            throw new InvalidOperationException("SDK Key/Secret manquants");
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
            throw new InvalidOperationException("SDK Key manquant");
        }
        return _sdkKey;
    }

    private static long ToUnixTimeSeconds(DateTime dateTime) =>
        (long)Math.Floor((dateTime - DateTime.UnixEpoch).TotalSeconds);

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public class ZoomTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class ZoomMeeting
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("start_url")]
    public string StartUrl { get; set; } = string.Empty;

    [JsonPropertyName("join_url")]
    public string JoinUrl { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("settings")]
    public ZoomMeetingSettings? Settings { get; set; }
}

public class ZoomMeetingSettings
{
    [JsonPropertyName("join_before_host")]
    public bool JoinBeforeHost { get; set; }

    [JsonPropertyName("waiting_room")]
    public bool WaitingRoom { get; set; }
}
