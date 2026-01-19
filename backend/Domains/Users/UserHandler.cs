using backend.Database.Models;
using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Users;

public class UserHandler(AppDbContext db) {
    private readonly AppDbContext _db = db;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);
    }

    public async Task<User?> ValidateCredentialsByEmailAsync(string email, string password, CancellationToken ct = default) {
        var user = await GetByEmailAsync(email, ct);
        return ValidatePassword(user, password);
    }

    public async Task<User?> ValidateCredentialsByUsernameAsync(string username, string password, CancellationToken ct = default) {
        var user = await GetByUsernameAsync(username, ct);
        return ValidatePassword(user, password);
    }

    private static User? ValidatePassword(User? user, string password) {
        if (user == null) return null;
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return isValid ? user : null;
    }

    public async Task<User> CreateUserAsync(string? username, string? email, string password, ProfileType profile, CancellationToken ct = default) {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User {
            Username = username,
            Email = email?.ToLower(),
            PasswordHash = passwordHash,
            Profile = profile,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return user;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default) {
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);
        return users.Select(u => new UserDto { Id = u.Id, Email = u.Email, Username = u.Username, Profile = u.Profile }).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default) {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (u == null) return null;
        return new UserDto { Id = u.Id, Email = u.Email, Username = u.Username, Profile = u.Profile };
    }

    public async Task<bool> UpdateRefreshTokenAsync(int userId, string refreshToken, DateTime expiry, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        user.RefreshTokenExpiry = expiry;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) {
        // Find all users with non-expired tokens, then verify hash
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

    public async Task<bool> RevokeRefreshTokenAsync(int userId, CancellationToken ct = default) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
