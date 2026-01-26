using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.Database;
using backend.Database.Models;
using backend.Domains.Zoom.Models;

namespace backend.Domains.Zoom;

public class ZoomService
{
    private readonly string? _accountId;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string? _sdkKey;
    private readonly string? _sdkSecret;
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _dbContext;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ZoomService(IConfiguration config, HttpClient httpClient, AppDbContext dbContext)
    {
        _accountId = Environment.GetEnvironmentVariable("ZOOM_ACCOUNT_ID") ?? config["Zoom:AccountId"];
        _clientId = Environment.GetEnvironmentVariable("ZOOM_CLIENT_ID") ?? config["Zoom:ClientId"];
        _clientSecret = Environment.GetEnvironmentVariable("ZOOM_CLIENT_SECRET") ?? config["Zoom:ClientSecret"];
        _sdkKey = Environment.GetEnvironmentVariable("ZOOM_SDK_KEY") ?? config["Zoom:SdkKey"];
        _sdkSecret = Environment.GetEnvironmentVariable("ZOOM_SDK_SECRET") ?? config["Zoom:SdkSecret"];
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.zoom.us/v2/");
        _dbContext = dbContext;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedAccessToken;
        }

        if (string.IsNullOrWhiteSpace(_accountId) || string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
        {
            throw new InvalidOperationException("Missing Zoom configuration (Account ID, Client ID, Client Secret)");
        }

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={_accountId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<ZoomTokenResponse>(content);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Unable to obtain Zoom access token");
        }

        _cachedAccessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5 min buffer

        return _cachedAccessToken;
    }

    public async Task<Meeting> CreateInstantMeetingAsync(Guid teacherId, Guid studentId, DateTime scheduledTime, string topic = "ApprendsMoi - Session")
    {
        var token = await GetAccessTokenAsync();
        
        // Reject past scheduled times
        if (scheduledTime <= DateTime.UtcNow)
        {
            throw new ArgumentException("Meeting time must be in the future");
        }
        
        var startTimeFormatted = scheduledTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        var meetingData = new
        {
            topic,
            type = 2, // Scheduled meeting (type 1 = instant has concurrency limits, type 2 = scheduled allows unlimited)
            start_time = startTimeFormatted,
            timezone = "UTC",
            duration = 60, // 1 hour duration
            settings = new
            {
                join_before_host = true,
                waiting_room = false,
                mute_upon_entry = false,
                approval_type = 2, // No registration required
                auto_recording = "none",
                allow_multiple_devices = true
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "users/me/meetings")
        {
            Content = new StringContent(JsonSerializer.Serialize(meetingData), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Error creating Zoom meeting: {error}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var zoomMeeting = JsonSerializer.Deserialize<ZoomMeeting>(content);

        if (zoomMeeting == null)
        {
            throw new InvalidOperationException("Unable to parse create meeting response");
        }
        
        var newMeeting = new Meeting
        {
            ZoomMeetingId = zoomMeeting.Id,
            Topic = zoomMeeting.Topic,
            JoinUrl = zoomMeeting.JoinUrl,
            StartUrl = zoomMeeting.StartUrl,
            Password = zoomMeeting.Password,
            CreatedAt = DateTime.UtcNow,
            ScheduledStartTime = NormalizeZoomStartTime(zoomMeeting.StartTime),
            Duration = zoomMeeting.Duration,
            TeacherId = teacherId,
            StudentId = studentId
        };
        
        _dbContext.Meetings.Add(newMeeting);
        await _dbContext.SaveChangesAsync();

        return newMeeting;
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

    private static DateTime NormalizeZoomStartTime(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        // Zoom can return start_time without timezone; treat it as local server time.
        return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    }
}

