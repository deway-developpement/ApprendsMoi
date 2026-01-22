using backend.Database.Models;
using backend.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace backend.Domains.Users;

public class UserAuthService(AppDbContext db) {
    private readonly AppDbContext _db = db;

    private static string HashRefreshToken(string refreshToken) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    public async Task<string?> GetByEmailAsync(string email, CancellationToken ct = default) {
        var normalizedEmail = email.ToLower();
        var exists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail, ct);
        return exists ? email : null;
    }

    public async Task<string?> GetByUsernameAsync(string username, CancellationToken ct = default) {
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Username == username, ct);
        return student?.Username;
    }

    public async Task<User?> ValidateCredentialsByEmailAsync(string email, string password, CancellationToken ct = default) {
        var normalizedEmail = email.ToLower();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        
        return ValidatePassword(user, password);
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

        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiry = expiry;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) {
        var tokenHash = HashRefreshToken(refreshToken);
        
        return await _db.Users
            .FirstOrDefaultAsync(
                u => u.RefreshTokenHash == tokenHash && u.RefreshTokenExpiry > DateTime.UtcNow,
                ct
            );
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
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return (null, null);

        if (role == ProfileType.Student) {
            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
            return (null, student?.Username);
        }
        
        return (user.Email, null);
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

    public async Task<bool> ParentExistsAsync(Guid parentId, CancellationToken ct = default) {
        return await _db.Parents.AnyAsync(p => p.UserId == parentId, ct);
    }
}
