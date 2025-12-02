namespace backend.Database.Models;

public class User {
    public int Id { get; set; }
    public string? Username { get; set; }
    public ProfileType Profile { get; set; } = ProfileType.Student;
}
