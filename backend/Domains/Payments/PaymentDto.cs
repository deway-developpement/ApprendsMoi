namespace backend.Domains.Payments;

public class BillingDto {
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public Guid ParentId { get; set; } // For authorization checks
    public Guid TeacherId { get; set; } // For authorization checks
    public decimal Amount { get; set; } // TTC - Total amount parent pays (includes commission)
    public decimal AmountHT { get; set; } // Hors Taxes
    public decimal VatAmount { get; set; } // Montant TVA
    public decimal TeacherEarning { get; set; } // Amount teacher receives (for teacher view only)
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? InvoiceNumber { get; set; }
}

public class PaymentDto {
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ParentId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CreatePaymentDto {
    public Guid InvoiceId { get; set; }
    public string Method { get; set; } = "CARD";
    public string? StripePaymentIntentId { get; set; }
}

public class PaymentHistoryDto {
    public List<BillingDto> Billings { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public decimal TotalPaid { get; set; } // For parents: sum of Amount, for teachers: sum of TeacherEarning
    public decimal TotalPending { get; set; } // For parents: sum of Amount, for teachers: sum of TeacherEarning
}
