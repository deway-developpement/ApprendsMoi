using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Subjects.Services;

public interface ISubjectService {
    Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto dto);
    Task<SubjectDto> GetSubjectByIdAsync(Guid subjectId);
    Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync();
    Task<SubjectDto> UpdateSubjectAsync(Guid subjectId, UpdateSubjectDto dto);
    Task DeleteSubjectAsync(Guid subjectId);
}

public class SubjectService : ISubjectService {
    private readonly AppDbContext _context;

    public SubjectService(AppDbContext context) {
        _context = context;
    }

    public async Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto dto) {
        // Check if subject with same name already exists
        var existingSubject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Name.ToLower() == dto.Name.ToLower());
        
        if (existingSubject != null) {
            throw new Exception("A subject with this name already exists");
        }

        var slug = GenerateSlug(dto.Name);

        var subject = new Subject {
            Name = dto.Name,
            Slug = slug
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return MapToDto(subject);
    }

    public async Task<SubjectDto> GetSubjectByIdAsync(Guid subjectId) {
        var subject = await _context.Subjects.FindAsync(subjectId);
        
        if (subject == null) {
            throw new Exception("Subject not found");
        }

        return MapToDto(subject);
    }

    public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync() {
        var subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync();

        return subjects.Select(MapToDto);
    }

    public async Task<SubjectDto> UpdateSubjectAsync(Guid subjectId, UpdateSubjectDto dto) {
        var subject = await _context.Subjects.FindAsync(subjectId);
        
        if (subject == null) {
            throw new Exception("Subject not found");
        }

        if (!string.IsNullOrEmpty(dto.Name)) {
            // Check if another subject with same name exists
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id != subjectId && s.Name.ToLower() == dto.Name.ToLower());
            
            if (existingSubject != null) {
                throw new Exception("A subject with this name already exists");
            }

            subject.Name = dto.Name;
            subject.Slug = GenerateSlug(dto.Name);
        }

        await _context.SaveChangesAsync();

        return MapToDto(subject);
    }

    public async Task DeleteSubjectAsync(Guid subjectId) {
        var subject = await _context.Subjects
            .Include(s => s.Courses)
            .Include(s => s.TeacherSubjects)
            .FirstOrDefaultAsync(s => s.Id == subjectId);
        
        if (subject == null) {
            throw new Exception("Subject not found");
        }

        // Check if subject is being used
        if (subject.Courses.Any()) {
            throw new Exception("Cannot delete subject that has associated courses");
        }

        if (subject.TeacherSubjects.Any()) {
            throw new Exception("Cannot delete subject that is taught by teachers");
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
    }

    private static SubjectDto MapToDto(Subject subject) {
        return new SubjectDto {
            Id = subject.Id,
            Name = subject.Name,
            Slug = subject.Slug
        };
    }

    private static string GenerateSlug(string name) {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("é", "e")
            .Replace("è", "e")
            .Replace("ê", "e")
            .Replace("à", "a")
            .Replace("â", "a")
            .Replace("ô", "o")
            .Replace("î", "i")
            .Replace("ï", "i")
            .Replace("ù", "u")
            .Replace("û", "u")
            .Replace("ç", "c");
    }
}
