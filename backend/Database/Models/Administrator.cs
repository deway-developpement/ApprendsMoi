namespace backend.Database.Models;

public class Administrator {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Email { get; set; } = string.Empty;
}
