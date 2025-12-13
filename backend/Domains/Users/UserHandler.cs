using backend.Database.Models;
using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Users;

public class UserHandler(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);
        return users.Select(u => new UserDto { Id = u.Id, Username = u.Username, Profile = u.Profile }).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u == null) return null;
        return new UserDto { Id = u.Id, Username = u.Username, Profile = u.Profile };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        var entity = new User
        {
            Username = req.Username,
            Profile = req.Profile ?? ProfileType.Student
        };

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new UserDto { Id = entity.Id, Username = entity.Username, Profile = entity.Profile };
    }
}
