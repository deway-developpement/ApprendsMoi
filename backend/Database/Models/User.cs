namespace backend.Database.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class User {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? Username { get; set; }
    public ProfileType Profile { get; set; } = ProfileType.Student;
}
