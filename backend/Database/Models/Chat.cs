namespace backend.Database.Models;

public class Chat {
    public Guid Id { get; set; }
    public ChatType ChatType { get; set; }
    
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    
    public Guid? ParentId { get; set; }
    public Parent? Parent { get; set; }
    
    public Guid? StudentId { get; set; }
    public Student? Student { get; set; }

    // Per-participant read tracking
    public DateTime? TeacherLastReadAt { get; set; }
    public DateTime? ParentLastReadAt { get; set; }
    public DateTime? StudentLastReadAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<ChatAttachment> Attachments { get; set; } = [];
}

public enum ChatType {
    ParentChat,
    StudentChat
}
