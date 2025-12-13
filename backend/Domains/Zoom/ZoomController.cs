using Microsoft.AspNetCore.Mvc;

namespace backend.Domains.Zoom;

[ApiController]
[Route("api/zoom")]
public class ZoomController : ControllerBase
{
    private readonly ZoomService _zoomService;

    public ZoomController(ZoomService zoomService)
    {
        _zoomService = zoomService;
    }

    /// <summary>
    /// Creates a new instant Zoom meeting
    /// </summary>
    [HttpPost("meeting")]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest? request)
    {
        try
        {
            var topic = request?.Topic ?? "ApprendsMoi - Session";
            var meeting = await _zoomService.CreateInstantMeetingAsync(topic);
            var hostSignature = _zoomService.GenerateSignature(meeting.Id.ToString(), 1);
            var participantSignature = _zoomService.GenerateSignature(meeting.Id.ToString(), 0);

            return Ok(new
            {
                meetingId = meeting.Id,
                meetingNumber = meeting.Id.ToString(),
                topic = meeting.Topic,
                joinUrl = meeting.JoinUrl,
                startUrl = meeting.StartUrl,
                password = meeting.Password,
                signature = hostSignature,
                hostSignature,
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
