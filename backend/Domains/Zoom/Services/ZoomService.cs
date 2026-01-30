using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Zoom;

public interface IZoomService
{
    Task<Meeting> CreateInstantMeetingAsync(Guid teacherId, Guid studentId, DateTime scheduledTime, int duration, string topic = "ApprendsMoi - Session", Guid? courseId = null);
    string GenerateSignature(string meetingNumber, int role = 0);
    string GetSdkKey();
}

public class ZoomService : IZoomService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _dbContext;
    private readonly ZoomTokenProvider _tokenProvider;
    private readonly ZoomSignatureService _signatureService;

    public ZoomService(HttpClient httpClient, AppDbContext dbContext, ZoomTokenProvider tokenProvider, ZoomSignatureService signatureService)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.zoom.us/v2/");
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
        _signatureService = signatureService;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        return await _tokenProvider.GetAccessTokenAsync();
    }

    public async Task<Meeting> CreateInstantMeetingAsync(Guid teacherId, Guid studentId, DateTime scheduledTime, int duration, string topic = "ApprendsMoi - Session", Guid? courseId = null)
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
            duration,
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
            StudentId = studentId,
            CourseId = courseId
        };
        
        _dbContext.Meetings.Add(newMeeting);
        await _dbContext.SaveChangesAsync();

        return newMeeting;
    }

    public string GenerateSignature(string meetingNumber, int role = 0)
    {
        return _signatureService.GenerateSignature(meetingNumber, role);
    }

    public string GetSdkKey()
    {
        return _signatureService.GetSdkKey();
    }

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
