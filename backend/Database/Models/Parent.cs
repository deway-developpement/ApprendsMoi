namespace backend.Database.Models;

public class Parent {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? AddressJson { get; set; }
    
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
