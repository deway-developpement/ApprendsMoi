using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using backend.Database.Models;

namespace backend.Domains.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController(
    UserProfileService profileService,
    UserAuthService authService,
    UserManagementService managementService) : ControllerBase {
    
    private readonly UserProfileService _profileService = profileService;
    private readonly UserAuthService _authService = authService;
    private readonly UserManagementService _managementService = managementService;

    [HttpGet]
    [AdminOnly]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserDto>>> Get(CancellationToken ct) {
        var list = await _profileService.GetAllUsersAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct) {
        var user = await _profileService.GetUserByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        // Update base user fields (FirstName, LastName, ProfilePicture)
        var baseUpdateSuccess = await _profileService.UpdateUserAsync(
            userId.Value,
            request.FirstName,
            request.LastName,
            request.ProfilePicture,
            ct
        );

        if (!baseUpdateSuccess) return NotFound(new { error = "User not found" });

        // Update role-specific fields based on the request data
        if (request.TeacherProfile != null) {
            var teacherSuccess = await _profileService.UpdateTeacherProfileAsync(
                userId.Value,
                request.TeacherProfile.Bio,
                request.TeacherProfile.PhoneNumber,
                request.TeacherProfile.City,
                request.TeacherProfile.TravelRadiusKm,
                ct
            );
            if (!teacherSuccess) return NotFound(new { error = "Teacher profile not found" });
        }

        if (request.ParentProfile != null) {
            var parentSuccess = await _profileService.UpdateParentProfileAsync(
                userId.Value,
                request.ParentProfile.PhoneNumber,
                ct
            );
            if (!parentSuccess) return NotFound(new { error = "Parent profile not found" });
        }

        if (request.StudentProfile != null) {
            var studentSuccess = await _profileService.UpdateStudentProfileAsync(
                userId.Value,
                request.StudentProfile.GradeLevel,
                request.StudentProfile.BirthDate,
                ct
            );
            if (!studentSuccess) return NotFound(new { error = "Student profile not found" });
        }

        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword)) {
            return BadRequest(new { error = "Invalid request" });
        }
        
        if (!JwtHelper.HasPasswordComplexity(request.NewPassword)) {
            return BadRequest(new { error = "Password must contain uppercase, lowercase, numbers, and be at least 6 characters long" });
        }

        var success = await _authService.UpdatePasswordAsync(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword,
            ct
        );

        if (!success) return BadRequest(new { error = "Current password is incorrect" });
        return Ok(new { message = "Password updated successfully" });
    }

    [HttpGet("students")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher, ProfileType.Parent)]
    public async Task<ActionResult<List<StudentDto>>> GetMyStudents(CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var userRole = JwtHelper.GetUserRoleFromClaims(User);
        
        if (userRole == ProfileType.Parent) {
            var students = await _profileService.GetStudentsByParentIdAsync(userId.Value, ct);
            return Ok(students);
        }
        else if (userRole == ProfileType.Teacher) {
            var students = await _profileService.GetStudentsByTeacherIdAsync(userId.Value, ct);
            return Ok(students);
        }
        else {
            var students = await _profileService.GetAllStudentsAsync(ct);
            return Ok(students);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        // Users can only deactivate their own account
        if (userId.Value != id) return Forbid();

        var success = await _managementService.DeactivateUserAsync(id, ct);
        if (!success) return NotFound(new { error = "User not found" });
        return Ok(new { message = "Account deactivated successfully" });
    }
}

public record UpdateProfileRequest(
    string? FirstName, 
    string? LastName, 
    string? ProfilePicture,
    TeacherProfileUpdate? TeacherProfile,
    ParentProfileUpdate? ParentProfile,
    StudentProfileUpdate? StudentProfile
);

public record TeacherProfileUpdate(string? Bio, string? PhoneNumber, string? City, int? TravelRadiusKm);
public record ParentProfileUpdate(string? PhoneNumber);
public record StudentProfileUpdate(GradeLevel? GradeLevel, DateOnly? BirthDate);
public record UpdatePasswordRequest(string CurrentPassword, string NewPassword);
