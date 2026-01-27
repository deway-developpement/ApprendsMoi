using backend.Database;
using backend.Database.Models;
using backend.Domains.Ratings;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Ratings.Services;

public interface IRatingService {
    Task<RatingDto> CreateRatingAsync(CreateRatingDto dto, Guid parentId);
    Task<RatingDto> GetRatingByIdAsync(Guid ratingId);
    Task<IEnumerable<RatingDto>> GetRatingsByTeacherIdAsync(Guid teacherId);
    Task<IEnumerable<RatingDto>> GetRatingsByParentIdAsync(Guid parentId);
    Task<RatingDto> UpdateRatingAsync(Guid ratingId, UpdateRatingDto dto, Guid parentId);
    Task DeleteRatingAsync(Guid ratingId, Guid parentId);
    Task<TeacherRatingStatsDto> GetTeacherRatingStatsAsync(Guid teacherId);
}

public class RatingService : IRatingService {
    private readonly AppDbContext _context;

    public RatingService(AppDbContext context) {
        _context = context;
    }

    public async Task<RatingDto> CreateRatingAsync(CreateRatingDto dto, Guid parentId) {
        if (dto.Rating < 1 || dto.Rating > 5) {
            throw new Exception("Rating must be between 1 and 5");
        }

        var teacher = await _context.Teachers.FindAsync(dto.TeacherId);
        if (teacher == null) {
            throw new Exception("Teacher not found");
        }

        var parent = await _context.Parents.FindAsync(parentId);
        if (parent == null) {
            throw new Exception("Parent not found");
        }

        if (dto.CourseId.HasValue) {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId && c.TeacherId == dto.TeacherId);
            
            if (course == null) {
                throw new Exception("Course not found or does not belong to this teacher");
            }

            if (course.Status != CourseStatus.COMPLETED) {
                throw new Exception("You can only rate a teacher after the course is completed");
            }

            // Verify parent owns the student in the course
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == course.StudentId && s.ParentId == parentId);
            
            if (student == null) {
                throw new Exception("Unauthorized: You can only rate teachers for your children's courses");
            }
        }

        // Check if parent has already rated this teacher
        var existingRating = await _context.TeacherRatings
            .FirstOrDefaultAsync(r => r.TeacherId == dto.TeacherId && r.ParentId == parentId);

        if (existingRating != null) {
            throw new Exception("You have already rated this teacher. Use update instead.");
        }

        var rating = new TeacherRating {
            TeacherId = dto.TeacherId,
            ParentId = parentId,
            CourseId = dto.CourseId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        _context.TeacherRatings.Add(rating);
        await _context.SaveChangesAsync();

        return await GetRatingByIdAsync(rating.Id);
    }

    public async Task<RatingDto> GetRatingByIdAsync(Guid ratingId) {
        var rating = await _context.TeacherRatings
            .Include(r => r.Teacher).ThenInclude(t => t.User)
            .Include(r => r.Parent).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == ratingId);

        if (rating == null) {
            throw new Exception("Rating not found");
        }

        return MapToDto(rating);
    }

    public async Task<IEnumerable<RatingDto>> GetRatingsByTeacherIdAsync(Guid teacherId) {
        var ratings = await _context.TeacherRatings
            .Include(r => r.Teacher).ThenInclude(t => t.User)
            .Include(r => r.Parent).ThenInclude(p => p.User)
            .Where(r => r.TeacherId == teacherId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return ratings.Select(MapToDto);
    }

    public async Task<IEnumerable<RatingDto>> GetRatingsByParentIdAsync(Guid parentId) {
        var ratings = await _context.TeacherRatings
            .Include(r => r.Teacher).ThenInclude(t => t.User)
            .Include(r => r.Parent).ThenInclude(p => p.User)
            .Where(r => r.ParentId == parentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return ratings.Select(MapToDto);
    }

    public async Task<RatingDto> UpdateRatingAsync(Guid ratingId, UpdateRatingDto dto, Guid parentId) {
        var rating = await _context.TeacherRatings.FindAsync(ratingId);
        if (rating == null) {
            throw new Exception("Rating not found");
        }

        if (rating.ParentId != parentId) {
            throw new Exception("Unauthorized: You can only update your own ratings");
        }

        if (dto.Rating.HasValue) {
            if (dto.Rating.Value < 1 || dto.Rating.Value > 5) {
                throw new Exception("Rating must be between 1 and 5");
            }
            rating.Rating = dto.Rating.Value;
        }

        if (dto.Comment != null) {
            rating.Comment = dto.Comment;
        }

        rating.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetRatingByIdAsync(ratingId);
    }

    public async Task DeleteRatingAsync(Guid ratingId, Guid parentId) {
        var rating = await _context.TeacherRatings.FindAsync(ratingId);
        if (rating == null) {
            throw new Exception("Rating not found");
        }

        if (rating.ParentId != parentId) {
            throw new Exception("Unauthorized: You can only delete your own ratings");
        }

        _context.TeacherRatings.Remove(rating);
        await _context.SaveChangesAsync();
    }

    public async Task<TeacherRatingStatsDto> GetTeacherRatingStatsAsync(Guid teacherId) {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == teacherId);

        if (teacher == null) {
            throw new Exception("Teacher not found");
        }

        var ratings = await _context.TeacherRatings
            .Include(r => r.Parent).ThenInclude(p => p.User)
            .Where(r => r.TeacherId == teacherId)
            .ToListAsync();

        var totalRatings = ratings.Count;
        var averageRating = totalRatings > 0 ? ratings.Average(r => r.Rating) : (double?)null;

        var ratingDistribution = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++) {
            ratingDistribution[i] = ratings.Count(r => r.Rating == i);
        }

        var recentRatings = ratings
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new AnonymousRatingDto {
                Rating = r.Rating,
                Comment = r.Comment
            })
            .ToList();

        return new TeacherRatingStatsDto {
            TeacherId = teacherId,
            TeacherName = $"{teacher.User.FirstName} {teacher.User.LastName}",
            AverageRating = averageRating.HasValue ? (decimal)averageRating.Value : null,
            TotalRatings = totalRatings,
            RatingDistribution = ratingDistribution,
            RecentRatings = recentRatings
        };
    }

    private RatingDto MapToDto(TeacherRating rating) {
        return new RatingDto {
            Id = rating.Id,
            TeacherId = rating.TeacherId,
            TeacherName = $"{rating.Teacher.User.FirstName} {rating.Teacher.User.LastName}",
            ParentId = rating.ParentId,
            ParentName = $"{rating.Parent.User.FirstName} {rating.Parent.User.LastName}",
            CourseId = rating.CourseId,
            Rating = rating.Rating,
            Comment = rating.Comment,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt
        };
    }
}
