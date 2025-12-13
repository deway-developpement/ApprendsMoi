using System;

namespace backend.Database.Models;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;
    public string PdfUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
