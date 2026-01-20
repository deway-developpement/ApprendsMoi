using System.Text.Json.Serialization;

namespace backend.Domains.Zoom.Models;

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

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

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
