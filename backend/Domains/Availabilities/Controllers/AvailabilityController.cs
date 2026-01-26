using Microsoft.AspNetCore.Mvc;
using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using backend.Domains.Availabilities.Services;

namespace backend.Domains.Availabilities.Controllers;

[ApiController]
[Route("api/availabilities")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly AvailabilityService _availabilityService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(AvailabilityService availabilityService, ILogger<AvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new availability slot for a teacher
    /// </summary>
    [HttpPost]
    [RequireRole(ProfileType.Teacher)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAvailability([FromBody] CreateAvailabilityRequest? request)
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

            // Parse time strings - handle both ISO 8601 DateTime format and TimeOnly format
            TimeOnly startTime;
            TimeOnly endTime;

            // Try parsing as DateTime first (handles ISO 8601 like "15:58:57.894Z")
            if (DateTime.TryParse(request.StartTime, out var startDateTime))
            {
                startTime = TimeOnly.FromDateTime(startDateTime);
            }
            else if (TimeOnly.TryParse(request.StartTime, out var parsedStartTime))
            {
                startTime = parsedStartTime;
            }
            else
            {
                return BadRequest(new { error = "Invalid StartTime format. Use HH:mm:ss or ISO 8601 format." });
            }

            if (DateTime.TryParse(request.EndTime, out var endDateTime))
            {
                endTime = TimeOnly.FromDateTime(endDateTime);
            }
            else if (TimeOnly.TryParse(request.EndTime, out var parsedEndTime))
            {
                endTime = parsedEndTime;
            }
            else
            {
                return BadRequest(new { error = "Invalid EndTime format. Use HH:mm:ss or ISO 8601 format." });
            }

            DateOnly? availabilityDate = null;
            if (request.AvailabilityDate.HasValue)
            {
                availabilityDate = DateOnly.FromDateTime(request.AvailabilityDate.Value);
            }

            var availability = await _availabilityService.CreateAvailabilityAsync(
                currentUserId.Value, 
                request.DayOfWeek!.Value, 
                startTime, 
                endTime, 
                request.IsRecurring,
                availabilityDate
            );

            return Ok(new AvailabilityResponse
            {
                Id = availability.Id,
                TeacherId = availability.TeacherId,
                DayOfWeek = availability.DayOfWeek,
                DayOfWeekName = ((DayOfWeek)availability.DayOfWeek).ToString(),
                AvailabilityDate = availability.AvailabilityDate,
                StartTime = availability.StartTime,
                EndTime = availability.EndTime,
                IsRecurring = availability.IsRecurring
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating availability.");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating availability.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Gets all availabilities for a specific teacher
    /// </summary>
    [HttpGet("teacher/{teacherId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTeacherAvailabilities(Guid teacherId)
    {
        try
        {
            // Verify the teacher exists
            var teacherExists = await _availabilityService.TeacherExistsAsync(teacherId);
            if (!teacherExists)
            {
                return NotFound(new { error = "Teacher not found" });
            }

            var availabilities = await _availabilityService.GetTeacherAvailabilitiesAsync(teacherId);

            var response = availabilities.Select(a => new AvailabilityResponse
            {
                Id = a.Id,
                TeacherId = a.TeacherId,
                DayOfWeek = a.DayOfWeek,
                DayOfWeekName = ((DayOfWeek)a.DayOfWeek).ToString(),
                AvailabilityDate = a.AvailabilityDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                IsRecurring = a.IsRecurring
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting teacher availabilities.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Blocks a specific time range (independent of availability slots)
    /// </summary>
    [HttpPost("block")]
    [RequireRole(ProfileType.Teacher)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UnavailableSlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BlockTime([FromBody] BlockAvailabilityRequest? request)
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

            // Parse time strings
            TimeOnly blockedStartTime;
            TimeOnly blockedEndTime;

            if (DateTime.TryParse(request.BlockedStartTime, out var startDateTime))
            {
                blockedStartTime = TimeOnly.FromDateTime(startDateTime);
            }
            else if (TimeOnly.TryParse(request.BlockedStartTime, out var parsedStartTime))
            {
                blockedStartTime = parsedStartTime;
            }
            else
            {
                return BadRequest(new { error = "Invalid BlockedStartTime format. Use HH:mm:ss or ISO 8601 format." });
            }

            if (DateTime.TryParse(request.BlockedEndTime, out var endDateTime))
            {
                blockedEndTime = TimeOnly.FromDateTime(endDateTime);
            }
            else if (TimeOnly.TryParse(request.BlockedEndTime, out var parsedEndTime))
            {
                blockedEndTime = parsedEndTime;
            }
            else
            {
                return BadRequest(new { error = "Invalid BlockedEndTime format. Use HH:mm:ss or ISO 8601 format." });
            }

            var unavailableSlot = await _availabilityService.BlockTimeAsync(
                currentUserId.Value,
                request.BlockedDate ?? DateTime.UtcNow,
                blockedStartTime,
                blockedEndTime,
                request.Reason
            );

            return Ok(new UnavailableSlotResponse
            {
                Id = unavailableSlot.Id,
                TeacherId = unavailableSlot.TeacherId,
                BlockedDate = unavailableSlot.BlockedDate,
                BlockedStartTime = unavailableSlot.BlockedStartTime,
                BlockedEndTime = unavailableSlot.BlockedEndTime,
                Reason = unavailableSlot.Reason,
                CreatedAt = unavailableSlot.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while blocking time.");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while blocking time.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Removes a blocked time slot
    /// </summary>
    [HttpDelete("block/{blockId}")]
    [RequireRole(ProfileType.Teacher)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveBlock(Guid blockId)
    {
        try
        {
            var currentUserId = JwtHelper.GetUserIdFromClaims(User);
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var removed = await _availabilityService.RemoveBlockAsync(blockId, currentUserId.Value);

            if (!removed)
            {
                return NotFound(new { error = "Block not found" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to remove block.");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while removing block.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Gets blocked time slots for a specific teacher (mirrors availability endpoint behavior)
    /// </summary>
    [HttpGet("block/teacher/{teacherId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<UnavailableSlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTeacherBlockedTimes(Guid teacherId)
    {
        try
        {
            var blocks = await _availabilityService.GetTeacherUnavailableSlotsAsync(teacherId);

            var response = blocks.Select(u => new UnavailableSlotResponse
            {
                Id = u.Id,
                TeacherId = u.TeacherId,
                BlockedDate = u.BlockedDate,
                BlockedStartTime = u.BlockedStartTime,
                BlockedEndTime = u.BlockedEndTime,
                Reason = u.Reason,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting blocked times for teacher.");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}

