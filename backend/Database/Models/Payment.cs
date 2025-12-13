using System;

namespace backend.Database.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string StripeIntentId { get; set; } = null!;
    public int AmountCents { get; set; }
    public string Status { get; set; } = "HELD";
    public DateTime CreatedAt { get; set; }

    public Invoice? Invoice { get; set; }
}
