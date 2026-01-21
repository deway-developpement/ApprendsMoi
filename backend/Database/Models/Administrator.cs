namespace backend.Database.Models;

/// <summary>
/// Extension pour les administrateurs
/// </summary>
public class Administrator {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Email { get; set; } = string.Empty;
}
