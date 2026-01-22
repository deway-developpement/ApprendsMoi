using backend.Database.Models;
using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Users;

public class UserManagementService(AppDbContext db) {
    private readonly AppDbContext _db = db;

    public async Task<User> CreateUserAsync(
        string email, 
        string password, 
        string firstName, 
        string lastName, 
        ProfileType profile,
        string? phoneNumber,
        CancellationToken ct = default) {
        
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User {
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            Profile = profile,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        if (profile == ProfileType.Teacher) {
            var teacher = new Teacher {
                User = user,
                Email = email.ToLower(),
                PhoneNumber = phoneNumber
            };
            _db.Teachers.Add(teacher);
        } else if (profile == ProfileType.Parent) {
            var parent = new Parent {
                User = user,
                Email = email.ToLower(),
                PhoneNumber = phoneNumber
            };
            _db.Parents.Add(parent);
        }

        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> CreateStudentAsync(
        Guid parentId,
        string username,
        string password,
        string firstName,
        string lastName,
        GradeLevel? gradeLevel,
        DateOnly? birthDate,
        CancellationToken ct = default) {
        
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User {
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            Profile = ProfileType.Student,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var student = new Student {
            User = user,
            Username = username,
            ParentId = parentId,
            GradeLevel = gradeLevel,
            BirthDate = birthDate
        };

        _db.Users.Add(user);
        _db.Students.Add(student);
        await _db.SaveChangesAsync(ct);
        
        return user;
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReactivateUserAsync(Guid userId, CancellationToken ct = default) {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.IsActive = true;
        user.DeletedAt = null;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> VerifyUserAsync(Guid userId, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.IsVerified = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateTeacherVerificationStatusAsync(Guid userId, VerificationStatus status, CancellationToken ct = default) {
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher == null) return false;

        teacher.VerificationStatus = status;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
