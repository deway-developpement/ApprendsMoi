using backend.Database.Models;
using backend.Database;
using MongoDB.Driver;

namespace backend.Domains.Users;

public class UserHandler(MongoDbContext db) {
    private readonly MongoDbContext _db = db;

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default) {
        var users = await _db.Users.Find(_ => true).ToListAsync(ct);
        return users.Select(u => new UserDto { Id = u.Id, Username = u.Username, Profile = u.Profile }).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default) {
        var u = await _db.Users.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (u == null) return null;
        return new UserDto { Id = u.Id, Username = u.Username, Profile = u.Profile };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest req, CancellationToken ct = default) {
        var entity = new User {
            Username = req.Username,
            Profile = req.Profile ?? ProfileType.Student
        };

        await _db.Users.InsertOneAsync(entity, cancellationToken: ct);

        return new UserDto { Id = entity.Id, Username = entity.Username, Profile = entity.Profile };
    }
}
