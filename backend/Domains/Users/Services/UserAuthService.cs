using backend.Database.Models;
using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Users;

public class UserAuthService(AppDbContext db) {
    private readonly AppDbContext _db = db;

    public async Task<string?> GetByEmailAsync(string email, CancellationToken ct = default) {
        var admin = await _db.Administrators.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email.ToLower(), ct);
        if (admin != null) return email;
        
        var teacher = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Email == email.ToLower(), ct);
        if (teacher != null) return email;
        
        var parent = await _db.Parents.AsNoTracking().FirstOrDefaultAsync(p => p.Email == email.ToLower(), ct);
        if (parent != null) return email;
        
        return null;
    }

    public async Task<string?> GetByUsernameAsync(string username, CancellationToken ct = default) {
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Username == username, ct);
        return student?.Username;
    }

    public async Task<User?> ValidateCredentialsByEmailAsync(string email, string password, CancellationToken ct = default) {
        var admin = await _db.Administrators.Include(a => a.User).FirstOrDefaultAsync(a => a.Email == email.ToLower(), ct);
        if (admin != null) return ValidatePassword(admin.User, password);
        
        var teacher = await _db.Teachers.Include(t => t.User).FirstOrDefaultAsync(t => t.Email == email.ToLower(), ct);
        if (teacher != null) return ValidatePassword(teacher.User, password);
        
        var parent = await _db.Parents.Include(p => p.User).FirstOrDefaultAsync(p => p.Email == email.ToLower(), ct);
        if (parent != null) return ValidatePassword(parent.User, password);
        
        return null;
    }

    public async Task<User?> ValidateCredentialsByUsernameAsync(string username, string password, CancellationToken ct = default) {
        var student = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Username == username, ct);
        return student != null ? ValidatePassword(student.User, password) : null;
    }

    private static User? ValidatePassword(User? user, string password) {
        if (user == null) return null;
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return isValid ? user : null;
    }

    public async Task<bool> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiry, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        user.RefreshTokenExpiry = expiry;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) {
        var users = await _db.Users
            .Where(u => u.RefreshTokenHash != null && u.RefreshTokenExpiry > DateTime.UtcNow)
            .ToListAsync(ct);
        
        foreach (var user in users) {
            if (BCrypt.Net.BCrypt.Verify(refreshToken, user.RefreshTokenHash)) {
                return user;
            }
        }
        
        return null;
    }

    public async Task<bool> RevokeRefreshTokenAsync(Guid userId, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(string? Email, string? Username)> GetUserCredentialsAsync(Guid userId, ProfileType role, CancellationToken ct = default) {
        string? email = null;
        string? username = null;

        if (role == ProfileType.Admin) {
            var admin = await _db.Administrators.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
            email = admin?.Email;
        } else if (role == ProfileType.Teacher) {
            var teacher = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == userId, ct);
            email = teacher?.Email;
        } else if (role == ProfileType.Parent) {
            var parent = await _db.Parents.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            email = parent?.Email;
        } else if (role == ProfileType.Student) {
            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
            username = student?.Username;
        }

        return (email, username);
    }

    public async Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
