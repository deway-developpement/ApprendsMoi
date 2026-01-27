namespace backend.Database.Models;

public class Message {
    public Guid Id { get; set; }
    
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<ChatAttachment> Attachments { get; set; } = [];
}
