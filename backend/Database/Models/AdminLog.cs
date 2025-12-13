using System;

namespace backend.Database.Models;

public class AdminLog
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public Administrator Admin { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public Guid? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
