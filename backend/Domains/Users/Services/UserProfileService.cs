using backend.Database.Models;
using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Users;

public class UserProfileService(AppDbContext db) {
    private readonly AppDbContext _db = db;

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default) {
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.Administrator)
            .Include(u => u.Teacher)
            .Include(u => u.Parent)
            .Include(u => u.Student)
            .ToListAsync(ct);

        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default) {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Administrator)
            .Include(u => u.Teacher)
            .Include(u => u.Parent)
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<bool> UpdateUserAsync(Guid userId, string? firstName, string? lastName, string? profilePicture, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        if (!string.IsNullOrEmpty(firstName)) user.FirstName = firstName;
        if (!string.IsNullOrEmpty(lastName)) user.LastName = lastName;
        if (profilePicture != null) user.ProfilePicture = profilePicture;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateTeacherProfileAsync(Guid userId, string? bio, string? phoneNumber, string? city, int? travelRadiusKm, CancellationToken ct = default) {
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher == null) return false;

        if (bio != null) teacher.Bio = bio;
        if (phoneNumber != null) teacher.PhoneNumber = phoneNumber;
        if (city != null) teacher.City = city;
        if (travelRadiusKm.HasValue) teacher.TravelRadiusKm = travelRadiusKm.Value;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateParentProfileAsync(Guid userId, string? phoneNumber, CancellationToken ct = default) {
        var parent = await _db.Parents.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (parent == null) return false;

        if (phoneNumber != null) parent.PhoneNumber = phoneNumber;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateStudentProfileAsync(Guid userId, GradeLevel? gradeLevel, DateOnly? birthDate, CancellationToken ct = default) {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (student == null) return false;

        if (gradeLevel.HasValue) student.GradeLevel = gradeLevel.Value;
        if (birthDate.HasValue) student.BirthDate = birthDate.Value;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<StudentDto>> GetStudentsByParentIdAsync(Guid parentId, CancellationToken ct = default) {
        var students = await _db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.ParentId == parentId)
            .ToListAsync(ct);

        return students.Select(s => new StudentDto {
            Id = s.User.Id,
            Username = s.Username,
            FirstName = s.User.FirstName,
            LastName = s.User.LastName,
            ProfilePicture = s.User.ProfilePicture,
            Profile = s.User.Profile,
            
            IsActive = s.User.IsActive,
            CreatedAt = s.User.CreatedAt,
            LastLoginAt = s.User.LastLoginAt,
            GradeLevel = s.GradeLevel,
            BirthDate = s.BirthDate,
            ParentId = s.ParentId
        }).ToList();
    }

    public async Task<List<StudentDto>> GetStudentsByTeacherIdAsync(Guid teacherId, CancellationToken ct = default) {
        var students = await _db.Courses
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .Select(c => c.Student)
            .Distinct()
            .Include(s => s.User)
            .ToListAsync(ct);

        return students.Select(s => new StudentDto {
            Id = s.User.Id,
            Username = s.Username,
            FirstName = s.User.FirstName,
            LastName = s.User.LastName,
            ProfilePicture = s.User.ProfilePicture,
            Profile = s.User.Profile,
            
            IsActive = s.User.IsActive,
            CreatedAt = s.User.CreatedAt,
            LastLoginAt = s.User.LastLoginAt,
            GradeLevel = s.GradeLevel,
            BirthDate = s.BirthDate,
            ParentId = s.ParentId
        }).ToList();
    }

    public async Task<List<StudentDto>> GetAllStudentsAsync(CancellationToken ct = default) {
        var students = await _db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .ToListAsync(ct);

        return students.Select(s => new StudentDto {
            Id = s.User.Id,
            Username = s.Username,
            FirstName = s.User.FirstName,
            LastName = s.User.LastName,
            ProfilePicture = s.User.ProfilePicture,
            Profile = s.User.Profile,
            
            IsActive = s.User.IsActive,
            CreatedAt = s.User.CreatedAt,
            LastLoginAt = s.User.LastLoginAt,
            GradeLevel = s.GradeLevel,
            BirthDate = s.BirthDate,
            ParentId = s.ParentId
        }).ToList();
    }

    public async Task<ParentDto?> GetParentByStudentIdAsync(Guid studentId, CancellationToken ct = default) {
        var student = await _db.Students
            .AsNoTracking()
            .Include(s => s.Parent)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.UserId == studentId, ct);

        if (student?.Parent == null) return null;

        var parent = student.Parent;
        return new ParentDto {
            Id = parent.User.Id,
            Email = parent.Email,
            FirstName = parent.User.FirstName,
            LastName = parent.User.LastName,
            ProfilePicture = parent.User.ProfilePicture,
            Profile = parent.User.Profile,
            IsActive = parent.User.IsActive,
            CreatedAt = parent.User.CreatedAt,
            LastLoginAt = parent.User.LastLoginAt,
            PhoneNumber = parent.PhoneNumber
        };
    }

    public async Task<List<TeacherDto>> GetAllTeachersAsync(CancellationToken ct = default) {
        var teachers = await _db.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .ToListAsync(ct);

        return teachers.Select(t => new TeacherDto {
            Id = t.User.Id,
            Email = t.Email,
            FirstName = t.User.FirstName,
            LastName = t.User.LastName,
            ProfilePicture = t.User.ProfilePicture,
            Profile = t.User.Profile,
            
            IsActive = t.User.IsActive,
            CreatedAt = t.User.CreatedAt,
            LastLoginAt = t.User.LastLoginAt,
            Bio = t.Bio,
            PhoneNumber = t.PhoneNumber,
            VerificationStatus = t.VerificationStatus,
            IsPremium = t.IsPremium,
            City = t.City,
            TravelRadiusKm = t.TravelRadiusKm
        }).ToList();
    }

    public async Task<List<TeacherDto>> GetTeachersByCityAsync(string city, CancellationToken ct = default) {
        var teachers = await _db.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.City != null && t.City.ToLower() == city.ToLower())
            .ToListAsync(ct);

        return teachers.Select(t => new TeacherDto {
            Id = t.User.Id,
            Email = t.Email,
            FirstName = t.User.FirstName,
            LastName = t.User.LastName,
            ProfilePicture = t.User.ProfilePicture,
            Profile = t.User.Profile,
            
            IsActive = t.User.IsActive,
            CreatedAt = t.User.CreatedAt,
            LastLoginAt = t.User.LastLoginAt,
            Bio = t.Bio,
            PhoneNumber = t.PhoneNumber,
            VerificationStatus = t.VerificationStatus,
            IsPremium = t.IsPremium,
            City = t.City,
            TravelRadiusKm = t.TravelRadiusKm
        }).ToList();
    }

    public async Task<bool> HasTeacherStudentRelationshipAsync(Guid teacherId, Guid studentId, CancellationToken ct = default) {
        // Check if teacher has taught this student (at least one course exists)
        return await _db.Courses
            .AnyAsync(c => c.TeacherId == teacherId && c.StudentId == studentId, ct);
    }

    private static UserDto MapToDto(User user) {
        string? email = null;
        string? username = null;

        if (user.Administrator != null) email = user.Administrator.Email;
        else if (user.Teacher != null) email = user.Teacher.Email;
        else if (user.Parent != null) email = user.Parent.Email;
        else if (user.Student != null) username = user.Student.Username;

        return user.Profile switch {
            ProfileType.Teacher when user.Teacher != null => new TeacherDto {
                Id = user.Id,
                Email = email,
                Username = username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                Profile = user.Profile,
                
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Bio = user.Teacher.Bio,
                PhoneNumber = user.Teacher.PhoneNumber,
                VerificationStatus = user.Teacher.VerificationStatus,
                IsPremium = user.Teacher.IsPremium,
                City = user.Teacher.City,
                TravelRadiusKm = user.Teacher.TravelRadiusKm
            },
            ProfileType.Parent when user.Parent != null => new ParentDto {
                Id = user.Id,
                Email = email,
                Username = username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                Profile = user.Profile,
                
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                PhoneNumber = user.Parent.PhoneNumber,
                StripeCustomerId = user.Parent.StripeCustomerId
            },
            ProfileType.Student when user.Student != null => new StudentDto {
                Id = user.Id,
                Username = username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                Profile = user.Profile,
                
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                GradeLevel = user.Student.GradeLevel,
                BirthDate = user.Student.BirthDate,
                ParentId = user.Student.ParentId
            },
            ProfileType.Admin when user.Administrator != null => new AdminDto {
                Id = user.Id,
                Email = email,
                Username = username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                Profile = user.Profile,
                
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            },
            _ => new UserDto {
                Id = user.Id,
                Email = email,
                Username = username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                Profile = user.Profile,
                
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }
}
