using backend.Domains.Courses;
using backend.Domains.Courses.Services;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Database.Models;

namespace backend.Domains.Courses.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase {
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService) {
        _courseService = courseService;
    }

    [HttpPost]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseDto dto) {
        try {
            var userProfile = User.FindFirst("profile")?.Value;
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // If teacher, ensure they can only create courses for themselves
            if (userProfile == ProfileType.Teacher.ToString() && dto.TeacherId != userId) {
                return Forbid();
            }

            var course = await _courseService.CreateCourseAsync(dto);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseDto>> GetCourse(Guid id) {
        try {
            var course = await _courseService.GetCourseByIdAsync(id);
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Check authorization
            if (userProfile != ProfileType.Admin.ToString() && 
                course.TeacherId != userId && 
                course.StudentId != userId) {
                // Check if user is parent of student
                var student = await _courseService.GetCourseByIdAsync(course.StudentId);
                return Forbid();
            }

            return Ok(course);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    [RequireRole(ProfileType.Admin)]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAllCourses() {
        try {
            var courses = await _courseService.GetAllCoursesAsync();
            return Ok(courses);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesByTeacher(Guid teacherId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var courses = await _courseService.GetCoursesByTeacherIdAsync(teacherId);
            return Ok(courses);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesByStudent(Guid studentId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Students can see their own, parents can see their children's, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && studentId != userId) {
                // TODO: Check if user is parent of student
                return Forbid();
            }

            var courses = await _courseService.GetCoursesByStudentIdAsync(studentId);
            return Ok(courses);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<CourseDto>> UpdateCourse(Guid id, [FromBody] UpdateCourseDto dto) {
        try {
            var course = await _courseService.GetCourseByIdAsync(id);
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only update their own courses
            if (userProfile != ProfileType.Admin.ToString() && course.TeacherId != userId) {
                return Forbid();
            }

            var updatedCourse = await _courseService.UpdateCourseAsync(id, dto);
            return Ok(updatedCourse);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<IActionResult> DeleteCourse(Guid id) {
        try {
            var course = await _courseService.GetCourseByIdAsync(id);
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only delete their own courses
            if (userProfile != ProfileType.Admin.ToString() && course.TeacherId != userId) {
                return Forbid();
            }

            await _courseService.DeleteCourseAsync(id);
            return NoContent();
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/attendance")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<CourseDto>> MarkAttendance(Guid id, [FromBody] MarkAttendanceDto dto) {
        try {
            var course = await _courseService.GetCourseByIdAsync(id);
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only mark attendance for their own courses
            if (userProfile != ProfileType.Admin.ToString() && course.TeacherId != userId) {
                return Forbid();
            }

            var updatedCourse = await _courseService.MarkAttendanceAsync(id, dto);
            return Ok(updatedCourse);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stats/student/{studentId}")]
    public async Task<ActionResult<CourseStatsDto>> GetStudentStats(Guid studentId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Students can see their own, parents can see their children's, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && studentId != userId) {
                // TODO: Check if user is parent of student
                return Forbid();
            }

            var stats = await _courseService.GetStudentStatsAsync(studentId);
            return Ok(stats);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stats/teacher/{teacherId}")]
    public async Task<ActionResult<CourseStatsDto>> GetTeacherStats(Guid teacherId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var stats = await _courseService.GetTeacherStatsAsync(teacherId);
            return Ok(stats);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("earnings/teacher/{teacherId}")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<TeacherEarningsDto>> GetTeacherEarnings(Guid teacherId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only see their own earnings, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var earnings = await _courseService.GetTeacherEarningsAsync(teacherId);
            return Ok(earnings);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
