using System;

namespace backend.Database.Models;

public class Report
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAt { get; set; }
}
