using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.TeacherSubjects.Services;

public interface ITeacherSubjectService {
    Task<TeacherSubjectDto> CreateTeacherSubjectAsync(Guid teacherId, CreateTeacherSubjectDto dto);
    Task<TeacherSubjectDto> GetTeacherSubjectAsync(Guid teacherId, Guid subjectId);
    Task<IEnumerable<TeacherSubjectDto>> GetTeacherSubjectsByTeacherAsync(Guid teacherId);
    Task<IEnumerable<TeacherSubjectDto>> GetTeachersBySubjectAsync(Guid subjectId);
    Task<TeacherSubjectDto> UpdateTeacherSubjectAsync(Guid teacherId, Guid subjectId, UpdateTeacherSubjectDto dto);
    Task DeleteTeacherSubjectAsync(Guid teacherId, Guid subjectId);
}

public class TeacherSubjectService : ITeacherSubjectService {
    private readonly AppDbContext _context;

    public TeacherSubjectService(AppDbContext context) {
        _context = context;
    }

    public async Task<TeacherSubjectDto> CreateTeacherSubjectAsync(Guid teacherId, CreateTeacherSubjectDto dto) {
        // Verify teacher exists
        var teacher = await _context.Teachers.FindAsync(teacherId);
        if (teacher == null) {
            throw new Exception("Teacher not found");
        }

        // Verify subject exists
        var subject = await _context.Subjects.FindAsync(dto.SubjectId);
        if (subject == null) {
            throw new Exception("Subject not found");
        }

        // Check if teacher already teaches this subject
        var existing = await _context.TeacherSubjects
            .FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == dto.SubjectId);
        
        if (existing != null) {
            throw new Exception("Teacher already teaches this subject");
        }

        var teacherSubject = new TeacherSubject {
            TeacherId = teacherId,
            SubjectId = dto.SubjectId,
            LevelMin = null,
            LevelMax = null,
            PricePerHour = dto.PricePerHour
        };

        _context.TeacherSubjects.Add(teacherSubject);
        await _context.SaveChangesAsync();

        return await GetTeacherSubjectAsync(teacherId, dto.SubjectId);
    }

    public async Task<TeacherSubjectDto> GetTeacherSubjectAsync(Guid teacherId, Guid subjectId) {
        var teacherSubject = await _context.TeacherSubjects
            .Include(ts => ts.Teacher).ThenInclude(t => t.User)
            .Include(ts => ts.Subject)
            .FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);

        if (teacherSubject == null) {
            throw new Exception("TeacherSubject not found");
        }

        return MapToDto(teacherSubject);
    }

    public async Task<IEnumerable<TeacherSubjectDto>> GetTeacherSubjectsByTeacherAsync(Guid teacherId) {
        var teacherSubjects = await _context.TeacherSubjects
            .Include(ts => ts.Teacher).ThenInclude(t => t.User)
            .Include(ts => ts.Subject)
            .Where(ts => ts.TeacherId == teacherId)
            .ToListAsync();

        return teacherSubjects.Select(MapToDto);
    }

    public async Task<IEnumerable<TeacherSubjectDto>> GetTeachersBySubjectAsync(Guid subjectId) {
        var teacherSubjects = await _context.TeacherSubjects
            .Include(ts => ts.Teacher).ThenInclude(t => t.User)
            .Include(ts => ts.Subject)
            .Where(ts => ts.SubjectId == subjectId)
            .ToListAsync();

        return teacherSubjects.Select(MapToDto);
    }

    public async Task<TeacherSubjectDto> UpdateTeacherSubjectAsync(Guid teacherId, Guid subjectId, UpdateTeacherSubjectDto dto) {
        var teacherSubject = await _context.TeacherSubjects
            .FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);

        if (teacherSubject == null) {
            throw new Exception("TeacherSubject not found");
        }

        if (dto.PricePerHour.HasValue) {
            teacherSubject.PricePerHour = dto.PricePerHour.Value;
        }

        await _context.SaveChangesAsync();

        return await GetTeacherSubjectAsync(teacherId, subjectId);
    }

    public async Task DeleteTeacherSubjectAsync(Guid teacherId, Guid subjectId) {
        var teacherSubject = await _context.TeacherSubjects
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.Courses)
            .FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);

        if (teacherSubject == null) {
            throw new Exception("TeacherSubject not found");
        }

        // Check if there are any courses using this teacher-subject combination
        var hasActiveCourses = teacherSubject.Teacher.Courses
            .Any(c => c.SubjectId == subjectId && c.Status != CourseStatus.CANCELLED);

        if (hasActiveCourses) {
            throw new Exception("Cannot delete teacher subject with active courses");
        }

        _context.TeacherSubjects.Remove(teacherSubject);
        await _context.SaveChangesAsync();
    }

    private static TeacherSubjectDto MapToDto(TeacherSubject teacherSubject) {
        return new TeacherSubjectDto {
            TeacherId = teacherSubject.TeacherId,
            TeacherName = $"{teacherSubject.Teacher.User.FirstName} {teacherSubject.Teacher.User.LastName}",
            SubjectId = teacherSubject.SubjectId,
            SubjectName = teacherSubject.Subject.Name,
            PricePerHour = teacherSubject.PricePerHour
        };
    }
}
