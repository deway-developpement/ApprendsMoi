namespace backend.Database.Models;

public class User {
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public ProfileType Profile { get; set; } = ProfileType.Student;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    
    public Administrator? Administrator { get; set; }
    public Parent? Parent { get; set; }
    public Teacher? Teacher { get; set; }
    public Student? Student { get; set; }
}

