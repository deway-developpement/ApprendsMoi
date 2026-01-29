using backend.Domains.Ratings;
using backend.Domains.Ratings.Services;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Database.Models;

namespace backend.Domains.Ratings.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RatingsController : ControllerBase {
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService) {
        _ratingService = ratingService;
    }

    [HttpPost]
    [RequireRole(ProfileType.Parent)]
    public async Task<ActionResult<RatingDto>> CreateRating([FromBody] CreateRatingDto dto) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var rating = await _ratingService.CreateRatingAsync(dto, userId);
            return CreatedAtAction(nameof(GetRating), new { id = rating.Id }, rating);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RatingDto>> GetRating(Guid id) {
        try {
            var rating = await _ratingService.GetRatingByIdAsync(id);
            return Ok(rating);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetRatingsByTeacher(Guid teacherId) {
        try {
            var ratings = await _ratingService.GetRatingsByTeacherIdAsync(teacherId);
            return Ok(ratings);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("parent/{parentId}")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetRatingsByParent(Guid parentId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Parents can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && parentId != userId) {
                return Forbid();
            }

            var ratings = await _ratingService.GetRatingsByParentIdAsync(parentId);
            return Ok(ratings);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stats/teacher/{teacherId}")]
    public async Task<ActionResult<TeacherRatingStatsDto>> GetTeacherRatingStats(Guid teacherId) {
        try {
            var stats = await _ratingService.GetTeacherRatingStatsAsync(teacherId);
            return Ok(stats);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [RequireRole(ProfileType.Parent)]
    public async Task<ActionResult<RatingDto>> UpdateRating(Guid id, [FromBody] UpdateRatingDto dto) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var rating = await _ratingService.UpdateRatingAsync(id, dto, userId);
            return Ok(rating);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ParentOrAdmin]
    public async Task<IActionResult> DeleteRating(Guid id) {
        try {
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            await _ratingService.DeleteRatingAsync(id, userId.Value, userProfile.Value);
            return NoContent();
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
