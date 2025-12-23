using Microsoft.AspNetCore.Mvc;
using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Zoom;

[ApiController]
[Route("api/zoom")]
public class ZoomController : ControllerBase
{
    private readonly ZoomService _zoomService;
    private readonly AppDbContext _dbContext;

    public ZoomController(ZoomService zoomService, AppDbContext dbContext)
    {
        _zoomService = zoomService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new instant Zoom meeting and saves it to the database
    /// </summary>
    [HttpPost("meeting")]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest? request)
    {
        try
        {
            var topic = request?.Topic ?? "ApprendsMoi - Session";
            var meeting = await _zoomService.CreateInstantMeetingAsync(topic);
            var participantSignature = _zoomService.GenerateSignature(meeting.Id.ToString(), 0);

            // Save meeting to database
            var dbMeeting = new Meeting
            {
                ZoomMeetingId = meeting.Id,
                Topic = meeting.Topic,
                JoinUrl = meeting.JoinUrl,
                StartUrl = meeting.StartUrl,
                Password = meeting.Password ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                ScheduledStartTime = meeting.StartTime,
                Duration = meeting.Duration
            };

            _dbContext.Meetings.Add(dbMeeting);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                id = dbMeeting.Id,
                meetingId = meeting.Id,
                meetingNumber = meeting.Id.ToString(),
                topic = meeting.Topic,
                joinUrl = meeting.JoinUrl,
                startUrl = meeting.StartUrl,
                password = meeting.Password,
                participantSignature,
                sdkKey = _zoomService.GetSdkKey()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Erreur lors de la création de la réunion: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets all meetings from the database
    /// </summary>
    [HttpGet("meetings")]
    public async Task<IActionResult> GetMeetings()
    {
        try
        {
            var meetings = await _dbContext.Meetings
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(meetings);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Erreur lors de la récupération des réunions: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets a specific meeting by ID
    /// </summary>
    [HttpGet("meetings/{id}")]
    public async Task<IActionResult> GetMeeting(int id)
    {
        try
        {
            var meeting = await _dbContext.Meetings.FindAsync(id);
            
            if (meeting == null)
            {
                return NotFound(new { error = "Réunion non trouvée" });
            }

            var participantSignature = _zoomService.GenerateSignature(meeting.ZoomMeetingId.ToString(), 0);

            return Ok(new
            {
                id = meeting.Id,
                meetingId = meeting.ZoomMeetingId,
                meetingNumber = meeting.ZoomMeetingId.ToString(),
                topic = meeting.Topic,
                joinUrl = meeting.JoinUrl,
                startUrl = meeting.StartUrl,
                password = meeting.Password,
                createdAt = meeting.CreatedAt,
                scheduledStartTime = meeting.ScheduledStartTime,
                duration = meeting.Duration,
                participantSignature,
                sdkKey = _zoomService.GetSdkKey()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Erreur lors de la récupération de la réunion: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generates signature for an existing meeting
    /// </summary>
    [HttpPost("signature")]
    public IActionResult GenerateSignature([FromBody] ZoomSignatureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MeetingNumber))
        {
            return BadRequest(new { error = "meetingNumber requis" });
        }

        try
        {
            var signature = _zoomService.GenerateSignature(request.MeetingNumber, request.Role);
            return Ok(new { signature, sdkKey = _zoomService.GetSdkKey() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Erreur lors de la génération de la signature: {ex.Message}" });
        }
    }
}

public class CreateMeetingRequest
{
    public string? Topic { get; set; }
}

public class ZoomSignatureRequest
{
    public string MeetingNumber { get; set; } = string.Empty;
    public int Role { get; set; } = 0;
}
