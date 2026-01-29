using backend.Domains.Courses;
using backend.Domains.Courses.Services;
using backend.Domains.Users;
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
    private readonly UserManagementService _userService;

    public CoursesController(ICourseService courseService, UserManagementService userService) {
        _courseService = courseService;
        _userService = userService;
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
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Check authorization
            if (userProfile != ProfileType.Admin && 
                course.TeacherId != userId && 
                course.StudentId != userId) {
                // Check if user is parent of student
                var student = await _userService.GetStudentWithParentAsync(course.StudentId);
                if (student == null || userProfile != ProfileType.Parent || student.ParentId != userId) {
                    return Forbid();
                }
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
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Students can see their own, parents can see their children's, admins can see all
            if (userProfile != ProfileType.Admin && studentId != userId) {
                // Check if user is parent of this student
                var student = await _userService.GetStudentWithParentAsync(studentId);
                if (student == null) {
                    return NotFound(new { message = "Student not found" });
                }
                
                if (userProfile != ProfileType.Parent || student.ParentId != userId) {
                    return Forbid();
                }
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
}
