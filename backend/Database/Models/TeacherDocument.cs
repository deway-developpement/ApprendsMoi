using System;

namespace backend.Database.Models;

public class TeacherDocument
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string Status { get; set; } = "PENDING";
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; }
}
