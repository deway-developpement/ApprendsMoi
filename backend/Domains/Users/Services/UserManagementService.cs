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
        
        var normalizedEmail = email.ToLower();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        
        try {
            var user = new User {
                FirstName = firstName,
                LastName = lastName,
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                Profile = profile,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);

            if (profile == ProfileType.Teacher) {
                var teacher = new Teacher {
                    User = user,
                    Email = normalizedEmail,
                    PhoneNumber = phoneNumber
                };
                _db.Teachers.Add(teacher);
            } else if (profile == ProfileType.Parent) {
                var parent = new Parent {
                    User = user,
                    Email = normalizedEmail,
                    PhoneNumber = phoneNumber
                };
                _db.Parents.Add(parent);
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            
            return user;
        } catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_users_email") == true) {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Email is already registered.", ex);
        } catch {
            await transaction.RollbackAsync(ct);
            throw;
        }
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
            Email = null,
            PasswordHash = passwordHash,
            Profile = ProfileType.Student,
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
        var user = await _db.Users
            .Include(u => u.Teacher)
            .Include(u => u.Student)
            .Include(u => u.Parent)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        // Cascade delete related entities
        if (user.Teacher != null) {
            // Delete teacher-specific data
            var teacherCourses = await _db.Courses.Where(c => c.TeacherId == userId).ToListAsync(ct);
            _db.Courses.RemoveRange(teacherCourses);
            
            var teacherAvailabilities = await _db.Availabilities.Where(a => a.TeacherId == userId).ToListAsync(ct);
            _db.Availabilities.RemoveRange(teacherAvailabilities);
            
            var teacherUnavailableSlots = await _db.UnavailableSlots.Where(u => u.TeacherId == userId).ToListAsync(ct);
            _db.UnavailableSlots.RemoveRange(teacherUnavailableSlots);
            
            var teacherSubjects = await _db.TeacherSubjects.Where(ts => ts.TeacherId == userId).ToListAsync(ct);
            _db.TeacherSubjects.RemoveRange(teacherSubjects);
            
            var teacherMeetings = await _db.Meetings.Where(m => m.TeacherId == userId).ToListAsync(ct);
            _db.Meetings.RemoveRange(teacherMeetings);
        }
        
        if (user.Student != null) {
            // Delete student-specific data
            var studentCourses = await _db.Courses.Where(c => c.StudentId == userId).ToListAsync(ct);
            _db.Courses.RemoveRange(studentCourses);
            
            var studentMeetings = await _db.Meetings.Where(m => m.StudentId == userId).ToListAsync(ct);
            _db.Meetings.RemoveRange(studentMeetings);
        }
        
        if (user.Parent != null) {
            // Delete all children of this parent
            var children = await _db.Students.Where(s => s.ParentId == userId).ToListAsync(ct);
            foreach (var child in children) {
                // Recursively delete each child (this will handle their courses, meetings, etc.)
                await DeactivateUserAsync(child.UserId, ct);
            }
        }

        // Invalidate refresh token to prevent new access tokens
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;
        
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReactivateUserAsync(Guid userId, CancellationToken ct = default) {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || user.DeletedAt == null) return false;

        user.IsActive = true;
        user.DeletedAt = null;
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

    public async Task<Student?> GetStudentWithParentAsync(Guid studentId, CancellationToken ct = default) {
        return await _db.Students
            .Include(s => s.Parent)
            .FirstOrDefaultAsync(s => s.UserId == studentId, ct);
    }
}
