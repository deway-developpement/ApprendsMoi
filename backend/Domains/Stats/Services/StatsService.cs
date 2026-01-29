using backend.Database;
using backend.Database.Models;
using backend.Domains.Ratings.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Stats.Services;

public interface IStatsService {
    Task<AdminStatsDto> GetAdminStatsAsync();
    Task<TeacherStatsDto> GetTeacherStatsAsync(Guid teacherId);
    Task<ParentStatsDto> GetParentStatsAsync(Guid parentId);
    Task<StudentStatsDto> GetStudentStatsAsync(Guid studentId);
}

public class StatsService : IStatsService {
    private readonly AppDbContext _context;
    private readonly IRatingService _ratingService;

    public StatsService(AppDbContext context, IRatingService ratingService) {
        _context = context;
        _ratingService = ratingService;
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync() {
        var now = DateTime.UtcNow;
        var startOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var endOfLastMonth = new DateTime(now.Year, now.Month, 1).AddSeconds(-1);
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1);

        // Count users active last month (logged in during that period)
        var activeUsersLastMonth = await _context.Users
            .Where(u => u.LastLoginAt.HasValue && 
                       u.LastLoginAt >= startOfLastMonth && 
                       u.LastLoginAt <= endOfLastMonth &&
                       u.IsActive)
            .CountAsync();

        // Sum commissions from completed courses this month
        var commissionsThisMonth = await _context.Courses
            .Where(c => c.Status == CourseStatus.COMPLETED &&
                       c.EndDate >= startOfThisMonth &&
                       c.EndDate <= now)
            .SumAsync(c => c.CommissionSnapshot);

        // Count completed courses this month
        var completedCoursesThisMonth = await _context.Courses
            .Where(c => c.Status == CourseStatus.COMPLETED &&
                       c.EndDate >= startOfThisMonth &&
                       c.EndDate <= now)
            .CountAsync();

        return new AdminStatsDto {
            ActiveUsersLastMonth = activeUsersLastMonth,
            CommissionsThisMonth = commissionsThisMonth,
            CompletedCoursesThisMonth = completedCoursesThisMonth
        };
    }

    public async Task<TeacherStatsDto> GetTeacherStatsAsync(Guid teacherId) {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == teacherId);

        if (teacher == null) {
            throw new Exception("Teacher not found");
        }

        // Get average rating and number of reviewers from Rating service
        var ratingStats = await _ratingService.GetTeacherRatingStatsAsync(teacherId);

        // Calculate earnings this month from invoices
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1);

        var earningsThisMonth = await _context.Invoices
            .Include(i => i.Course)
            .Where(i => i.Course.TeacherId == teacherId &&
                       i.Status == InvoiceStatus.PAID &&
                       i.PaidAt.HasValue &&
                       i.PaidAt >= startOfThisMonth &&
                       i.PaidAt <= now)
            .SumAsync(i => i.TeacherEarning);

        // Count current students following (students with at least one completed or upcoming course)
        var currentStudents = await _context.Courses
            .Where(c => c.TeacherId == teacherId &&
                       (c.Status == CourseStatus.COMPLETED || 
                        c.Status == CourseStatus.CONFIRMED ||
                        c.Status == CourseStatus.PENDING))
            .Select(c => c.StudentId)
            .Distinct()
            .CountAsync();

        return new TeacherStatsDto {
            AverageRating = ratingStats.AverageRating,
            NumberOfReviewers = ratingStats.TotalRatings,
            EarningsThisMonth = earningsThisMonth,
            CurrentStudentsFollowing = currentStudents
        };
    }

    public async Task<ParentStatsDto> GetParentStatsAsync(Guid parentId) {
        var parent = await _context.Parents
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == parentId);

        if (parent == null) {
            throw new Exception("Parent not found");
        }

        // Calculate total debt (pending invoices)
        var totalDebt = await _context.Invoices
            .Where(i => i.ParentId == parentId && i.Status == InvoiceStatus.PENDING)
            .SumAsync(i => i.Amount);

        // Count courses booked this month (courses created this month for their children)
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1);

        var studentIds = await _context.Students
            .Where(s => s.ParentId == parentId)
            .Select(s => s.UserId)
            .ToListAsync();

        var coursesBookedThisMonth = await _context.Courses
            .Where(c => studentIds.Contains(c.StudentId) &&
                       c.CreatedAt >= startOfThisMonth &&
                       c.CreatedAt <= now)
            .CountAsync();

        // Count number of children registered
        var numberOfChildren = await _context.Students
            .Where(s => s.ParentId == parentId)
            .CountAsync();

        return new ParentStatsDto {
            TotalDebt = totalDebt,
            CoursesBookedThisMonth = coursesBookedThisMonth,
            NumberOfChildren = numberOfChildren
        };
    }

    public async Task<StudentStatsDto> GetStudentStatsAsync(Guid studentId) {
        var student = await _context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == studentId);

        if (student == null) {
            throw new Exception("Student not found");
        }

        // Calculate total hours in completed courses this month
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1);

        var coursesThisMonth = await _context.Courses
            .Where(c => c.StudentId == studentId &&
                       c.Status == CourseStatus.COMPLETED &&
                       c.EndDate >= startOfThisMonth &&
                       c.EndDate <= now)
            .ToListAsync();

        var totalHoursThisMonth = coursesThisMonth.Sum(c => c.DurationMinutes) / 60.0m;
        var numberOfCoursesThisMonth = coursesThisMonth.Count;

        return new StudentStatsDto {
            TotalHoursThisMonth = totalHoursThisMonth,
            NumberOfCoursesThisMonth = numberOfCoursesThisMonth
        };
    }
}
