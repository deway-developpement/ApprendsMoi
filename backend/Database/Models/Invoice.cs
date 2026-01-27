namespace backend.Database.Models;

public class Invoice {
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal Commission { get; set; }
    public decimal TeacherEarning { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.PENDING;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? InvoiceNumber { get; set; }
}
