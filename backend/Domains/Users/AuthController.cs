using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using backend.Database.Models;
using System.Text.RegularExpressions;

namespace backend.Domains.Users;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ILogger<AuthController> logger, UserHandler userHandler) : ControllerBase {
    private readonly ILogger<AuthController> _logger = logger;
    private readonly UserHandler _userHandler = userHandler;
    
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]{3,20}$", RegexOptions.Compiled);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) {
        if (string.IsNullOrEmpty(request.Credential) || string.IsNullOrEmpty(request.Password)) {
            return BadRequest(new { error = "Credential and password are required" });
        }

        if (request.IsStudent) {
            if (!UsernameRegex.IsMatch(request.Credential)) {
                return BadRequest(new { error = "Invalid username format" });
            }
        } else {
            if (!EmailRegex.IsMatch(request.Credential)) {
                return BadRequest(new { error = "Invalid email format" });
            }
        }

        User? user;
        
        if (request.IsStudent) {
            user = await _userHandler.ValidateCredentialsByUsernameAsync(request.Credential, request.Password, ct);
            if (user == null || user.Profile != ProfileType.Student) {
                return Unauthorized(new { error = "Invalid student credentials" });
            }
        } else {
            user = await _userHandler.ValidateCredentialsByEmailAsync(request.Credential, request.Password, ct);
            if (user == null || user.Profile == ProfileType.Student) {
                return Unauthorized(new { error = "Invalid credentials" });
            }
        }

        var token = JwtHelper.GenerateToken(user.Id, user.Email, user.Username ?? "", user.Profile);
        var refreshToken = JwtHelper.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddHours(4);

        await _userHandler.UpdateRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry, ct);

        return Ok(new LoginResponse(
            token,
            refreshToken,
            new UserDto {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Profile = user.Profile
            }
        ));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct) {
        if (request.Profile == null) {
            return BadRequest(new { error = "Profile type is required" });
        }
        
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6) {
            return BadRequest(new { error = "Password is incorrect" });
        }

        var userProfileType = request.Profile ?? ProfileType.Student;
        
        if (userProfileType == ProfileType.Student) {
            if (string.IsNullOrEmpty(request.Username) || !UsernameRegex.IsMatch(request.Username) || request.Username.Length < 3) {
                return BadRequest(new { error = "Username is incorrect" });
            }
            
            var existingStudent = await _userHandler.GetByUsernameAsync(request.Username, ct);
            if (existingStudent != null) {
                return Conflict(new { error = "Username already exists" });
            }
        } else {
            if (string.IsNullOrEmpty(request.Email) || !EmailRegex.IsMatch(request.Email) || request.Email.Length < 5) {
                return BadRequest(new { error = "Email is incorrect" });
            }
            
            var existingUser = await _userHandler.GetByEmailAsync(request.Email, ct);
            if (existingUser != null) {
                return Conflict(new { error = "Email already exists" });
            }
        }

        var username = userProfileType == ProfileType.Student
            ? request.Username!
            : null;
        var email = userProfileType != ProfileType.Student
            ? request.Email?.ToLower()!
            : null;
        var user = await _userHandler.CreateUserAsync(
            username,
            email,
            request.Password,
            userProfileType,
            ct
        );

        return CreatedAtAction(
            nameof(GetCurrentUser),
            new { id = user.Id },
            new {
                id = user.Id,
                message = "User registered successfully"
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

        var user = await _userHandler.GetUserByIdAsync(userId.Value, ct);
        if (user == null) {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new UserDto {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Profile = user.Profile
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct) {
        if (string.IsNullOrEmpty(request.RefreshToken)) {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var user = await _userHandler.GetByRefreshTokenAsync(request.RefreshToken, ct);
        if (user == null) {
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }

        var newAccessToken = JwtHelper.GenerateToken(user.Id, user.Email, user.Username ?? "", user.Profile);
        var newRefreshToken = JwtHelper.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddHours(4);

        await _userHandler.UpdateRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiry, ct);

        return Ok(new LoginResponse(
            newAccessToken,
            newRefreshToken,
            new UserDto {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Profile = user.Profile
            }
        ));
    }
}

public record LoginRequest(string Credential, string Password, bool IsStudent);
public record RegisterRequest(string? Email, string Password, string? Username, ProfileType? Profile);
public record LoginResponse(string Token, string RefreshToken, UserDto User);
public record RefreshTokenRequest(string RefreshToken);
