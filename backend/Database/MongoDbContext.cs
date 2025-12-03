using MongoDB.Driver;
using backend.Database.Models;

namespace backend.Database;

public class MongoDbContext {
    private readonly IMongoDatabase _db;

    public MongoDbContext(MongoDbSettings settings) {
        var client = new MongoClient(settings.ConnectionString);
        _db = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<User> Users => _db.GetCollection<User>("users");
}
