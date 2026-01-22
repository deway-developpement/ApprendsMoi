using Microsoft.AspNetCore.Mvc;
using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;
using backend.Domains.Zoom.Models;
using Microsoft.AspNetCore.Http;

namespace backend.Domains.Zoom;

[ApiController]
[Route("api/zoom")]
public class ZoomController : ControllerBase
{
    private readonly ZoomService _zoomService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ZoomController> _logger;

    public ZoomController(ZoomService zoomService, AppDbContext dbContext, ILogger<ZoomController> logger)
    {
        _zoomService = zoomService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new instant Zoom meeting and saves it to the database
    /// </summary>
    [HttpPost("meeting")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateMeetingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest? request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _dbContext.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return BadRequest(new { error = "User not found" });
            }

            var topic = request.Topic ?? "ApprendsMoi - Session";
            
            var meeting = await _zoomService.CreateInstantMeetingAsync(request.UserId, topic);
            var participantSignature = _zoomService.GenerateSignature(meeting.ZoomMeetingId.ToString(), 0);

            return Ok(new CreateMeetingResponse
            {
                Id = meeting.Id,
                MeetingId = meeting.ZoomMeetingId,
                MeetingNumber = meeting.ZoomMeetingId.ToString(),
                Topic = meeting.Topic,
                JoinUrl = meeting.JoinUrl,
                StartUrl = meeting.StartUrl,
                Password = meeting.Password,
                ParticipantSignature = participantSignature,
                SdkKey = _zoomService.GetSdkKey()
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating meeting.");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while creating meeting.");
            return StatusCode(502, new { error = "An error occurred while communicating with an external service." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating a meeting.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Gets all meetings from the database
    /// </summary>
    [HttpGet("meetings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Meeting>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            _logger.LogError(ex, "An unexpected error occurred while getting all meetings.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Gets a specific meeting by ID
    /// </summary>
    [HttpGet("meetings/{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MeetingDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMeeting(int id)
    {
        try
        {
            var meeting = await _dbContext.Meetings.FindAsync(id);
            
            if (meeting == null)
            {
                return NotFound(new { error = "Meeting not found" });
            }

            var participantSignature = _zoomService.GenerateSignature(meeting.ZoomMeetingId.ToString(), 0);

            return Ok(new MeetingDetailsResponse
            {
                Id = meeting.Id,
                MeetingId = meeting.ZoomMeetingId,
                MeetingNumber = meeting.ZoomMeetingId.ToString(),
                Topic = meeting.Topic,
                JoinUrl = meeting.JoinUrl,
                StartUrl = meeting.StartUrl,
                Password = meeting.Password,
                CreatedAt = meeting.CreatedAt,
                ScheduledStartTime = meeting.ScheduledStartTime,
                Duration = meeting.Duration,
                ParticipantSignature = participantSignature,
                SdkKey = _zoomService.GetSdkKey()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting a meeting by id.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
    
    /// <summary>
    /// Generates signature for an existing meeting
    /// </summary>
    [HttpPost("signature")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SignatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateSignature([FromBody] ZoomSignatureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MeetingNumber))
        {
            return BadRequest(new { error = "meetingNumber is required" });
        }

        try
        {
            var signature = _zoomService.GenerateSignature(request.MeetingNumber, request.Role);
            return Ok(new SignatureResponse
            {
                Signature = signature,
                SdkKey = _zoomService.GetSdkKey()
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while generating signature.");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while generating signature.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}
