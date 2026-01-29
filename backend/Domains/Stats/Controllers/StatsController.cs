using backend.Database.Models;
using backend.Domains.Stats.Services;
using backend.Domains.Users;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Domains.Stats.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase {
    private readonly IStatsService _statsService;
    private readonly UserProfileService _profileService;
    private readonly UserManagementService _userManagementService;

    public StatsController(IStatsService statsService, UserProfileService profileService, UserManagementService userManagementService) {
        _statsService = statsService;
        _profileService = profileService;
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<StatsResponseDto>> GetStats([FromQuery] Guid? targetId = null, CancellationToken ct = default) {
        try {
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Determine which user's stats to fetch
            Guid effectiveUserId = userId.Value;
            ProfileType? effectiveProfile = userProfile;

            // Handle targetId parameter
            if (targetId.HasValue) {
                // Only admins and parents can use targetId
                if (userProfile == ProfileType.Admin) {
                    // Admin can fetch stats for any user
                    var targetUser = await _profileService.GetUserByIdAsync(targetId.Value, ct);
                    if (targetUser == null) {
                        return NotFound(new { message = "Target user not found" });
                    }
                    effectiveUserId = targetId.Value;
                    effectiveProfile = targetUser.Profile;
                }
                else if (userProfile == ProfileType.Parent) {
                    // Parents can only fetch stats for their own children
                    var student = await _userManagementService.GetStudentWithParentAsync(targetId.Value, ct);
                    if (student == null) {
                        return NotFound(new { message = "Student not found" });
                    }
                    
                    if (student.ParentId != userId) {
                        return Forbid();
                    }
                    
                    effectiveUserId = targetId.Value;
                    effectiveProfile = ProfileType.Student;
                }
                else {
                    // Teachers and students cannot use targetId
                    return Forbid();
                }
            }

            // Fetch stats based on effective user's profile
            object stats = effectiveProfile switch {
                ProfileType.Admin => await _statsService.GetAdminStatsAsync(ct),
                ProfileType.Teacher => await _statsService.GetTeacherStatsAsync(effectiveUserId, ct),
                ProfileType.Parent => await _statsService.GetParentStatsAsync(effectiveUserId, ct),
                ProfileType.Student => await _statsService.GetStudentStatsAsync(effectiveUserId, ct),
                _ => throw new InvalidOperationException("Invalid user profile")
            };

            return Ok(new StatsResponseDto {
                UserType = effectiveProfile.ToString()!,
                Stats = stats
            });
        }
        catch (Exception ex) when (ex.Message.Contains("not found")) {
            return NotFound(new { message = "Resource not found" });
        }
        catch (InvalidOperationException) {
            return BadRequest(new { message = "Invalid operation" });
        }
        catch (OperationCanceledException) {
            return StatusCode(499, new { message = "Request cancelled" });
        }
        catch (Exception) {
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}
