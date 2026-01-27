namespace backend.Database.Models;

public class ChatAttachment {
    public Guid Id { get; set; }
    
    public Guid? MessageId { get; set; }
    public Message? Message { get; set; }
    
    public Guid? ChatId { get; set; }
    public Chat? Chat { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    
    public Guid UploadedBy { get; set; }
    public User Uploader { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
