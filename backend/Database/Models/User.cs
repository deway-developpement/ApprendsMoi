namespace backend.Database.Models;

public class User {
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public ProfileType Profile { get; set; } = ProfileType.Student;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}

