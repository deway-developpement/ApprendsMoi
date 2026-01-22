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
    [Authorize]
    public async Task<ActionResult<List<UserDto>>> Get(CancellationToken ct) {
        var list = await _profileService.GetAllUsersAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
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

        var success = await _profileService.UpdateUserAsync(
            userId.Value,
            request.FirstName,
            request.LastName,
            request.ProfilePicture,
            ct
        );

        if (!success) return NotFound(new { error = "User not found" });
        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpPut("profile/teacher")]
    [Authorize]
    public async Task<IActionResult> UpdateTeacherProfile([FromBody] UpdateTeacherProfileRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var success = await _profileService.UpdateTeacherProfileAsync(
            userId.Value,
            request.Bio,
            request.PhoneNumber,
            request.City,
            request.TravelRadiusKm,
            ct
        );

        if (!success) return NotFound(new { error = "Teacher profile not found" });
        return Ok(new { message = "Teacher profile updated successfully" });
    }

    [HttpPut("profile/parent")]
    [Authorize]
    public async Task<IActionResult> UpdateParentProfile([FromBody] UpdateParentProfileRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var success = await _profileService.UpdateParentProfileAsync(
            userId.Value,
            request.PhoneNumber,
            ct
        );

        if (!success) return NotFound(new { error = "Parent profile not found" });
        return Ok(new { message = "Parent profile updated successfully" });
    }

    [HttpPut("profile/student")]
    [Authorize]
    public async Task<IActionResult> UpdateStudentProfile([FromBody] UpdateStudentProfileRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var success = await _profileService.UpdateStudentProfileAsync(
            userId.Value,
            request.GradeLevel,
            request.BirthDate,
            ct
        );

        if (!success) return NotFound(new { error = "Student profile not found" });
        return Ok(new { message = "Student profile updated successfully" });
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword)) {
            return BadRequest(new { error = "Invalid request" });
        }

        if (request.NewPassword.Length < 6) {
            return BadRequest(new { error = "Password must be at least 6 characters" });
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
    [Authorize]
    public async Task<ActionResult<List<StudentDto>>> GetMyStudents(CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) return Unauthorized();

        var students = await _profileService.GetStudentsByParentIdAsync(userId.Value, ct);
        return Ok(students);
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

public record UpdateProfileRequest(string? FirstName, string? LastName, string? ProfilePicture);
public record UpdateTeacherProfileRequest(string? Bio, string? PhoneNumber, string? City, int? TravelRadiusKm);
public record UpdateParentProfileRequest(string? PhoneNumber);
public record UpdateStudentProfileRequest(GradeLevel? GradeLevel, DateOnly? BirthDate);
public record UpdatePasswordRequest(string CurrentPassword, string NewPassword);
