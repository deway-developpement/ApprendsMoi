using backend.Database;
using backend.Database.Models;
using backend.Domains.Courses;
using backend.Domains.Payments.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Courses.Services;

public interface ICourseService {
    Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
    Task<CourseDto> GetCourseByIdAsync(Guid courseId);
    Task<IEnumerable<CourseDto>> GetCoursesByTeacherIdAsync(Guid teacherId);
    Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(Guid studentId);
    Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
    Task<CourseDto> UpdateCourseAsync(Guid courseId, UpdateCourseDto dto);
    Task DeleteCourseAsync(Guid courseId);
}

public class CourseService : ICourseService {
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;

    public CourseService(AppDbContext context, IPaymentService paymentService) {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto) {
        var teacher = await _context.Teachers
            .Include(t => t.TeacherSubjects)
            .FirstOrDefaultAsync(t => t.UserId == dto.TeacherId);
        
        if (teacher == null) {
            throw new Exception("Teacher not found");
        }

        var student = await _context.Students.FindAsync(dto.StudentId);
        if (student == null) {
            throw new Exception("Student not found");
        }

        var subject = await _context.Subjects.FindAsync(dto.SubjectId);
        if (subject == null) {
            throw new Exception("Subject not found");
        }

        var teacherSubject = teacher.TeacherSubjects.FirstOrDefault(ts => ts.SubjectId == dto.SubjectId);
        if (teacherSubject == null) {
            throw new Exception("Teacher doesn't teach this subject");
        }

        var platformCommissionRate = 0.15m; // We take 15% commission

        var format = Enum.Parse<CourseFormat>(dto.Format);
        var pricePerHour = teacherSubject.PricePerHour;
        var durationHours = dto.DurationMinutes / 60.0m;
        var price = pricePerHour * durationHours;
        var commission = price * platformCommissionRate;

        var course = new Course {
            TeacherId = dto.TeacherId,
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            Format = format,
            StartDate = dto.StartDate,
            EndDate = dto.StartDate.AddMinutes(dto.DurationMinutes),
            DurationMinutes = dto.DurationMinutes,
            PriceSnapshot = price,
            CommissionSnapshot = commission,
            Status = CourseStatus.PENDING
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return await GetCourseByIdAsync(course.Id);
    }

    public async Task<CourseDto> GetCourseByIdAsync(Guid courseId) {
        var course = await _context.Courses
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.Subject)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) {
            throw new Exception("Course not found");
        }

        return MapToDto(course);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByTeacherIdAsync(Guid teacherId) {
        var courses = await _context.Courses
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.Subject)
            .Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(Guid studentId) {
        var courses = await _context.Courses
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.Subject)
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync() {
        var courses = await _context.Courses
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.Subject)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<CourseDto> UpdateCourseAsync(Guid courseId, UpdateCourseDto dto) {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) {
            throw new Exception("Course not found");
        }

        var previousStatus = course.Status;

        if (dto.StartDate.HasValue) {
            course.StartDate = dto.StartDate.Value;
            course.EndDate = dto.StartDate.Value.AddMinutes(course.DurationMinutes);
        }

        if (dto.DurationMinutes.HasValue) {
            course.DurationMinutes = dto.DurationMinutes.Value;
            course.EndDate = course.StartDate.AddMinutes(dto.DurationMinutes.Value);
        }

        if (!string.IsNullOrEmpty(dto.Status)) {
            course.Status = Enum.Parse<CourseStatus>(dto.Status);
            
            // Auto-mark student as attended when course is completed
            if (course.Status == CourseStatus.COMPLETED && previousStatus != CourseStatus.COMPLETED) {
                course.StudentAttended = true;
                course.AttendanceMarkedAt = DateTime.UtcNow;
            }
        }

        if (dto.MeetingLink != null) {
            course.MeetingLink = dto.MeetingLink;
        }

        await _context.SaveChangesAsync();

        // Auto-create billing when course is marked as COMPLETED
        if (course.Status == CourseStatus.COMPLETED && previousStatus != CourseStatus.COMPLETED) {
            try {
                await _paymentService.CreateBillingForCourseAsync(courseId);
            }
            catch (Exception ex) {
                // Log but don't fail the course update if billing creation fails
                Console.WriteLine($"Warning: Failed to create billing for course {courseId}: {ex.Message}");
            }
        }

        return await GetCourseByIdAsync(courseId);
    }

    public async Task DeleteCourseAsync(Guid courseId) {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) {
            throw new Exception("Course not found");
        }

        if (course.Status == CourseStatus.COMPLETED) {
            throw new Exception("Cannot delete completed courses");
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
    }

    private CourseDto MapToDto(Course course) {
        return new CourseDto {
            Id = course.Id,
            TeacherId = course.TeacherId,
            TeacherName = $"{course.Teacher.User.FirstName} {course.Teacher.User.LastName}",
            StudentId = course.StudentId,
            StudentName = $"{course.Student.User.FirstName} {course.Student.User.LastName}",
            SubjectId = course.SubjectId,
            SubjectName = course.Subject.Name,
            Status = course.Status.ToString(),
            Format = course.Format.ToString(),
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            DurationMinutes = course.DurationMinutes,
            PriceSnapshot = course.PriceSnapshot,
            CommissionSnapshot = course.CommissionSnapshot,
            MeetingLink = course.MeetingLink,
            StudentAttended = course.StudentAttended,
            AttendanceMarkedAt = course.AttendanceMarkedAt,
            CreatedAt = course.CreatedAt
        };
    }
}
