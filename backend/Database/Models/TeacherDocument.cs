namespace backend.Database.Models;

public class TeacherDocument {
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public DocumentStatus Status { get; set; } = DocumentStatus.PENDING;
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
}
