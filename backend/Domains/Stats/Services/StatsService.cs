using backend.Database;
using backend.Database.Models;
using backend.Domains.Ratings.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Stats.Services;

public interface IStatsService {
    Task<AdminStatsDto> GetAdminStatsAsync(CancellationToken ct = default);
    Task<TeacherStatsDto> GetTeacherStatsAsync(Guid teacherId, CancellationToken ct = default);
    Task<ParentStatsDto> GetParentStatsAsync(Guid parentId, CancellationToken ct = default);
    Task<StudentStatsDto> GetStudentStatsAsync(Guid studentId, CancellationToken ct = default);
}

public class StatsService : IStatsService {
    private readonly AppDbContext _context;
    private readonly IRatingService _ratingService;

    public StatsService(AppDbContext context, IRatingService ratingService) {
        _context = context;
        _ratingService = ratingService;
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync(CancellationToken ct = default) {
        var now = DateTime.UtcNow;
        var startOfLastMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        var endOfLastMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-1);
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Count users active last month (logged in during that period)
        var activeUsersLastMonth = await _context.Users
            .Where(u => u.IsActive &&
                       u.LastLoginAt != null && 
                       u.LastLoginAt.Value >= startOfLastMonth && 
                       u.LastLoginAt.Value <= endOfLastMonth)
            .CountAsync(ct);

        // Sum commissions from completed courses this month
        var commissionsThisMonth = await _context.Courses
            .Where(c => c.Status == CourseStatus.COMPLETED &&
                       c.EndDate >= startOfThisMonth &&
                       c.EndDate <= now)
            .SumAsync(c => c.CommissionSnapshot, ct);

        // Count completed courses this month
        var completedCoursesThisMonth = await _context.Courses
            .Where(c => c.Status == CourseStatus.COMPLETED &&
                       c.EndDate >= startOfThisMonth &&
                       c.EndDate <= now)
            .CountAsync(ct);

        return new AdminStatsDto {
            ActiveUsersLastMonth = activeUsersLastMonth,
            CommissionsThisMonth = commissionsThisMonth,
            CompletedCoursesThisMonth = completedCoursesThisMonth
        };
    }

    public async Task<TeacherStatsDto> GetTeacherStatsAsync(Guid teacherId, CancellationToken ct = default) {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == teacherId, ct);

        if (teacher == null) {
            throw new Exception("Teacher not found");
        }

        // Get average rating and number of reviewers from Rating service
        var ratingStats = await _ratingService.GetTeacherRatingStatsAsync(teacherId);

        // Calculate earnings this month from invoices
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var earningsThisMonth = await _context.Invoices
            .Where(i => _context.Courses.Any(c => c.Id == i.CourseId && c.TeacherId == teacherId) &&
                       i.Status == InvoiceStatus.PAID &&
                       i.PaidAt != null &&
                       i.PaidAt.Value >= startOfThisMonth &&
                       i.PaidAt.Value <= now)
            .SumAsync(i => i.TeacherEarning, ct);

        // Count current students following (students with at least one completed or upcoming course)
        var currentStudents = await _context.Courses
            .Where(c => c.TeacherId == teacherId &&
                       (c.Status == CourseStatus.COMPLETED || 
                        c.Status == CourseStatus.CONFIRMED ||
                        c.Status == CourseStatus.PENDING))
            .Select(c => c.StudentId)
            .Distinct()
            .CountAsync(ct);

        return new TeacherStatsDto {
            AverageRating = ratingStats.AverageRating,
            NumberOfReviewers = ratingStats.TotalRatings,
            EarningsThisMonth = earningsThisMonth,
            CurrentStudentsFollowing = currentStudents
        };
    }

    public async Task<ParentStatsDto> GetParentStatsAsync(Guid parentId, CancellationToken ct = default) {
        var parent = await _context.Parents
            .FirstOrDefaultAsync(p => p.UserId == parentId, ct);

        if (parent == null) {
            throw new Exception("Parent not found");
        }

        // Calculate total debt (pending invoices)
        var totalDebt = await _context.Invoices
            .Where(i => i.ParentId == parentId && i.Status == InvoiceStatus.PENDING)
            .SumAsync(i => i.Amount, ct);

        // Count courses booked this month (courses created this month for their children)
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var coursesBookedThisMonth = await _context.Courses
            .Where(c =>
                _context.Students.Any(s => s.ParentId == parentId && s.UserId == c.StudentId) &&
                c.CreatedAt >= startOfThisMonth &&
                c.CreatedAt <= now)
            .CountAsync(ct);

        // Count number of children registered
        var numberOfChildren = await _context.Students
            .Where(s => s.ParentId == parentId)
            .CountAsync(ct);

        return new ParentStatsDto {
            TotalDebt = totalDebt,
            CoursesBookedThisMonth = coursesBookedThisMonth,
            NumberOfChildren = numberOfChildren
        };
    }

    public async Task<StudentStatsDto> GetStudentStatsAsync(Guid studentId, CancellationToken ct = default) {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == studentId, ct);

        if (student == null) {
            throw new Exception("Student not found");
        }

        // Calculate total hours in completed courses this month
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var coursesThisMonthQuery = _context.Courses
            .Where(c => c.StudentId == studentId &&
                       c.Status == CourseStatus.COMPLETED &&
                       c.EndDate >= startOfThisMonth &&
                       c.EndDate <= now);
        var totalMinutesThisMonth = await coursesThisMonthQuery.SumAsync(c => c.DurationMinutes, ct);
        var totalHoursThisMonth = totalMinutesThisMonth / 60.0m;
        var numberOfCoursesThisMonth = await coursesThisMonthQuery.CountAsync(ct);

        return new StudentStatsDto {
            TotalHoursThisMonth = totalHoursThisMonth,
            NumberOfCoursesThisMonth = numberOfCoursesThisMonth
        };
    }
}
