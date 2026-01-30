using backend.Domains.TeacherSubjects.Services;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Database.Models;

namespace backend.Domains.TeacherSubjects.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherSubjectsController : ControllerBase {
    private readonly ITeacherSubjectService _teacherSubjectService;

    public TeacherSubjectsController(ITeacherSubjectService teacherSubjectService) {
        _teacherSubjectService = teacherSubjectService;
    }

    [HttpPost("teacher/{teacherId}")]
    [Authorize]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<TeacherSubjectDto>> CreateTeacherSubject(
        Guid teacherId, 
        [FromBody] CreateTeacherSubjectDto dto) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only add subjects for themselves
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var teacherSubject = await _teacherSubjectService.CreateTeacherSubjectAsync(teacherId, dto);
            return CreatedAtAction(
                nameof(GetTeacherSubject), 
                new { teacherId = teacherSubject.TeacherId, subjectId = teacherSubject.SubjectId }, 
                teacherSubject);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("teacher/{teacherId}/subject/{subjectId}")]
    public async Task<ActionResult<TeacherSubjectDto>> GetTeacherSubject(Guid teacherId, Guid subjectId) {
        try {
            var teacherSubject = await _teacherSubjectService.GetTeacherSubjectAsync(teacherId, subjectId);
            return Ok(teacherSubject);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<IEnumerable<TeacherSubjectDto>>> GetTeacherSubjects(Guid teacherId) {
        try {
            var teacherSubjects = await _teacherSubjectService.GetTeacherSubjectsByTeacherAsync(teacherId);
            return Ok(teacherSubjects);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<IEnumerable<TeacherSubjectDto>>> GetTeachersBySubject(Guid subjectId) {
        try {
            var teacherSubjects = await _teacherSubjectService.GetTeachersBySubjectAsync(subjectId);
            return Ok(teacherSubjects);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("teacher/{teacherId}/subject/{subjectId}")]
    [Authorize]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<TeacherSubjectDto>> UpdateTeacherSubject(
        Guid teacherId, 
        Guid subjectId, 
        [FromBody] UpdateTeacherSubjectDto dto) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only update their own subjects
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var teacherSubject = await _teacherSubjectService.UpdateTeacherSubjectAsync(teacherId, subjectId, dto);
            return Ok(teacherSubject);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("teacher/{teacherId}/subject/{subjectId}")]
    [Authorize]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<IActionResult> DeleteTeacherSubject(Guid teacherId, Guid subjectId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only delete their own subjects
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            await _teacherSubjectService.DeleteTeacherSubjectAsync(teacherId, subjectId);
            return NoContent();
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
