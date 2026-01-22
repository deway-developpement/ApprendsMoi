using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using backend.Database.Models;
using System.Text.RegularExpressions;

namespace backend.Domains.Users;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ILogger<AuthController> logger, 
    UserAuthService authService,
    UserProfileService profileService,
    UserManagementService managementService) : ControllerBase {
    
    private readonly ILogger<AuthController> _logger = logger;
    private readonly UserAuthService _authService = authService;
    private readonly UserProfileService _profileService = profileService;
    private readonly UserManagementService _managementService = managementService;
    
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]{3,20}$", RegexOptions.Compiled);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) {
        if (string.IsNullOrEmpty(request.Credential) || string.IsNullOrEmpty(request.Password)) {
            return BadRequest(new { error = "Invalid credentials" });
        }

        if (request.IsStudent) {
            if (!UsernameRegex.IsMatch(request.Credential)) {
                return BadRequest(new { error = "Invalid credentials" });
            }
        } else {
            if (!EmailRegex.IsMatch(request.Credential)) {
                return BadRequest(new { error = "Invalid credentials" });
            }
        }

        User? user;
        
        if (request.IsStudent) {
            user = await _authService.ValidateCredentialsByUsernameAsync(request.Credential, request.Password, ct);
            if (user == null || user.Profile != ProfileType.Student) {
                return Unauthorized(new { error = "Invalid credentials" });
            }
        } else {
            user = await _authService.ValidateCredentialsByEmailAsync(request.Credential.ToLower(), request.Password, ct);
            if (user == null || user.Profile == ProfileType.Student) {
                return Unauthorized(new { error = "Invalid credentials" });
            }
        }

        var (email, username) = await _authService.GetUserCredentialsAsync(user.Id, user.Profile, ct);
        
        // Update last login timestamp
        await _authService.UpdateLastLoginAsync(user.Id, ct);
        
        var token = JwtHelper.GenerateToken(user.Id, email, username, user.Profile);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, ct);

        var userDto = await _profileService.GetUserByIdAsync(user.Id, ct);
        if (userDto == null) {
            return StatusCode(500, new { error = "Failed to retrieve user information" });
        }

        return Ok(new LoginResponse(token, refreshToken, userDto));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct) {
        if (request.Profile == null) {
            return BadRequest(new { error = "Registration failed" });
        }
        
        // Only Teachers and Parents can register via this endpoint
        // Admins are seeded, Students are created by parents via /register/student
        if (request.Profile.Value != ProfileType.Teacher && request.Profile.Value != ProfileType.Parent) {
            return BadRequest(new { error = "Registration failed" });
        }
        
        if (string.IsNullOrEmpty(request.Email) || !EmailRegex.IsMatch(request.Email)) {
            return BadRequest(new { error = "Registration failed" });
        }
        
        if (string.IsNullOrEmpty(request.Password) || !JwtHelper.HasPasswordComplexity(request.Password)) {
            return BadRequest(new { error = "Password must contain uppercase, lowercase, numbers, and be at least 6 characters long" });
        }

        if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName)) {
            return BadRequest(new { error = "Registration failed" });
        }

        // Check if email already exists
        var emailExists = await _authService.EmailExistsAsync(request.Email, ct);
        if (emailExists) {
            return Conflict(new { error = "Registration failed" });
        }

        var user = await _managementService.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Profile.Value,
            request.PhoneNumber,
            ct
        );

        return Ok(
            new {
                id = user.Id,
                message = "User registered successfully"
            }
        );
    }

    [HttpPost("register/student")]
    [ParentOrAdmin]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest request, CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);
        if (userId == null) {
            return Unauthorized(new { error = "Invalid token" });
        }

        var userRole = JwtHelper.GetUserRoleFromClaims(User);

        // Determine the parent ID based on the caller's role
        Guid parentId;
        if (userRole == ProfileType.Parent) {
            parentId = userId.Value;
        }
        else if (userRole == ProfileType.Admin) {
            if (request.ParentId == null) {
                return BadRequest(new { error = "Parent ID is required for admin registration" });
            }
            
            var parentExists = await _authService.ParentExistsAsync(request.ParentId.Value, ct);
            if (!parentExists) {
                return BadRequest(new { error = "Parent not found" });
            }
            
            parentId = request.ParentId.Value;
        }
        else {
            return Forbid();
        }

        if (string.IsNullOrEmpty(request.Username) || !UsernameRegex.IsMatch(request.Username)) {
            return BadRequest(new { error = "Registration failed" });
        }

        if (string.IsNullOrEmpty(request.Password) || !JwtHelper.HasPasswordComplexity(request.Password)) {
            return BadRequest(new { error = "Password must contain uppercase, lowercase, numbers, and be at least 6 characters long" });
        }

        if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName)) {
            return BadRequest(new { error = "Registration failed" });
        }

        var existingStudent = await _authService.GetByUsernameAsync(request.Username, ct);
        if (existingStudent != null) {
            return Conflict(new { error = "Registration failed" });
        }

        var student = await _managementService.CreateStudentAsync(
            parentId,
            request.Username,
            request.Password,
            request.FirstName,
            request.LastName,
            request.GradeLevel,
            request.BirthDate,
            ct
        );

        return Ok(
            new {
                id = student.Id,
                message = "Student registered successfully"
            }
        );
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct) {
        var userId = JwtHelper.GetUserIdFromClaims(User);

        if (userId == null) {
            return Unauthorized(new { error = "Invalid token" });
        }

        var user = await _profileService.GetUserByIdAsync(userId.Value, ct);
        if (user == null) {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct) {
        if (string.IsNullOrEmpty(request.RefreshToken)) {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var user = await _authService.GetByRefreshTokenAsync(request.RefreshToken, ct);
        if (user == null) {
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }

        var (email, username) = await _authService.GetUserCredentialsAsync(user.Id, user.Profile, ct);
        var newAccessToken = JwtHelper.GenerateToken(user.Id, email, username, user.Profile);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, ct);

        var userDto = await _profileService.GetUserByIdAsync(user.Id, ct);
        if (userDto == null) {
            return StatusCode(500, new { error = "Failed to retrieve user information" });
        }

        return Ok(new LoginResponse(newAccessToken, newRefreshToken, userDto));
    }

    private async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, CancellationToken ct = default) {
        var (refreshToken, expiry) = JwtHelper.GenerateRefreshTokenWithExpiry();
        await _authService.UpdateRefreshTokenAsync(userId, refreshToken, expiry, ct);
        return refreshToken;
    }
}

public record LoginRequest(string Credential, string Password, bool IsStudent);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, ProfileType? Profile, string? PhoneNumber);
public record RegisterStudentRequest(string Username, string Password, string FirstName, string LastName, GradeLevel? GradeLevel, DateOnly? BirthDate, Guid? ParentId);
public record LoginResponse(string Token, string RefreshToken, UserDto User);
public record RefreshTokenRequest(string RefreshToken);
