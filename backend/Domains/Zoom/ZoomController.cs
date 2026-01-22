using Microsoft.AspNetCore.Mvc;
using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;
using backend.Domains.Zoom.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using System.Security.Claims;

namespace backend.Domains.Zoom;

[ApiController]
[Route("api/zoom")]
[Authorize]
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

            var currentUserId = JwtHelper.GetUserIdFromClaims(User);
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == ProfileType.Parent.ToString() || userRole == ProfileType.Student.ToString())
            {
                return Forbid();
            }

            // Teachers can only create meetings where they are the teacher
            // Admins can create any meeting
            if (userRole == ProfileType.Teacher.ToString() && request.TeacherId != currentUserId)
            {
                return Forbid();
            }

            var teacher = await _dbContext.Users.FindAsync(request.TeacherId);
            if (teacher == null)
            {
                return BadRequest(new { error = "Teacher not found" });
            }

            var student = await _dbContext.Users.FindAsync(request.StudentId);
            if (student == null)
            {
                return BadRequest(new { error = "Student not found" });
            }

            var topic = request.Topic ?? "ApprendsMoi - Session";
            
            var meeting = await _zoomService.CreateInstantMeetingAsync(request.TeacherId, request.StudentId, topic);
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
                SdkKey = _zoomService.GetSdkKey(),
                TeacherId = meeting.TeacherId,
                StudentId = meeting.StudentId
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
    /// Gets all meetings (filtered by user role and ownership)
    /// </summary>
    [HttpGet("meetings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<MeetingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMeetings()
    {
        try
        {
            var currentUserId = JwtHelper.GetUserIdFromClaims(User);
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == ProfileType.Parent.ToString())
            {
                return Forbid();
            }

            IQueryable<Meeting> query = _dbContext.Meetings;

            // Filter based on role
            if (userRole == ProfileType.Teacher.ToString())
            {
                query = query.Where(m => m.TeacherId == currentUserId);
            }
            else if (userRole == ProfileType.Student.ToString())
            {
                query = query.Where(m => m.StudentId == currentUserId);
            }
            // Admin can see all meetings (no filter)

            var meetings = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var response = meetings.Select(m => new MeetingResponse
            {
                Id = m.Id,
                MeetingId = m.ZoomMeetingId,
                Topic = m.Topic,
                CreatedAt = NormalizeToUtc(m.CreatedAt),
                ScheduledStartTime = NormalizeToUtc(m.ScheduledStartTime),
                Duration = m.Duration,
                TeacherId = m.TeacherId,
                StudentId = m.StudentId
            }).ToList();

            return Ok(response);
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMeeting(int id)
    {
        try
        {
            var currentUserId = JwtHelper.GetUserIdFromClaims(User);
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == ProfileType.Parent.ToString())
            {
                return Forbid();
            }

            var meeting = await _dbContext.Meetings.FindAsync(id);
            
            if (meeting == null)
            {
                return NotFound(new { error = "Meeting not found" });
            }

            // Check access rights
            if (userRole != ProfileType.Admin.ToString())
            {
                // Teachers can only view their meetings
                if (userRole == ProfileType.Teacher.ToString() && meeting.TeacherId != currentUserId)
                {
                    return Forbid();
                }
                // Students can only view their meetings
                if (userRole == ProfileType.Student.ToString() && meeting.StudentId != currentUserId)
                {
                    return Forbid();
                }
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
                CreatedAt = NormalizeToUtc(meeting.CreatedAt),
                ScheduledStartTime = NormalizeToUtc(meeting.ScheduledStartTime),
                Duration = meeting.Duration,
                ParticipantSignature = participantSignature,
                SdkKey = _zoomService.GetSdkKey(),
                TeacherId = meeting.TeacherId,
                StudentId = meeting.StudentId
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

    private static DateTime NormalizeToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        // Treat database timestamps as UTC when kind is unspecified.
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        return value.HasValue ? NormalizeToUtc(value.Value) : null;
    }
}
