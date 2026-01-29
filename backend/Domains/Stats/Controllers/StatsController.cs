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
    public async Task<ActionResult<StatsResponseDto>> GetStats([FromQuery] Guid? targetId = null) {
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
                    var targetUser = await _profileService.GetUserByIdAsync(targetId.Value);
                    if (targetUser == null) {
                        return NotFound(new { message = "Target user not found" });
                    }
                    effectiveUserId = targetId.Value;
                    effectiveProfile = targetUser.Profile;
                }
                else if (userProfile == ProfileType.Parent) {
                    // Parents can only fetch stats for their own children
                    var student = await _userManagementService.GetStudentWithParentAsync(targetId.Value);
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
                ProfileType.Admin => await _statsService.GetAdminStatsAsync(),
                ProfileType.Teacher => await _statsService.GetTeacherStatsAsync(effectiveUserId),
                ProfileType.Parent => await _statsService.GetParentStatsAsync(effectiveUserId),
                ProfileType.Student => await _statsService.GetStudentStatsAsync(effectiveUserId),
                _ => throw new Exception("Invalid user profile")
            };

            return Ok(new StatsResponseDto {
                UserType = effectiveProfile.ToString()!,
                Stats = stats
            });
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
