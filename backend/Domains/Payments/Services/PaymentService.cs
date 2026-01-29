using backend.Database;
using backend.Database.Models;
using backend.Domains.Payments;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Payments.Services;

public interface IPaymentService {
    Task<BillingDto> CreateBillingForCourseAsync(Guid courseId);
    Task<BillingDto> GetBillingByIdAsync(Guid billingId);
    Task<IEnumerable<BillingDto>> GetBillingsByParentIdAsync(Guid parentId);
    Task<IEnumerable<BillingDto>> GetBillingsByTeacherIdAsync(Guid teacherId);
    Task<IEnumerable<BillingDto>> GetAllBillingsAsync();
    Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto, Guid parentId);
    Task<PaymentHistoryDto> GetParentPaymentHistoryAsync(Guid parentId);
    Task<PaymentHistoryDto> GetTeacherPaymentHistoryAsync(Guid teacherId);
    Task<byte[]> GenerateInvoicePdfAsync(Guid billingId);
    Task<Guid?> GetCourseTeacherIdAsync(Guid courseId);
}

public class PaymentService : IPaymentService {
    private readonly AppDbContext _context;
    private readonly IInvoicePdfService _pdfService;

    public PaymentService(AppDbContext context, IInvoicePdfService pdfService) {
        _context = context;
        _pdfService = pdfService;
    }

    public async Task<BillingDto> CreateBillingForCourseAsync(Guid courseId) {
        var course = await _context.Courses
            .Include(c => c.Student).ThenInclude(s => s.Parent)
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Subject)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) {
            throw new Exception("Course not found");
        }

        // Check if billing record already exists
        var existingInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.CourseId == courseId);
        
        if (existingInvoice != null) {
            return await GetBillingByIdAsync(existingInvoice.Id);
        }

        var teacherEarning = course.PriceSnapshot - course.CommissionSnapshot;
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        // Calculate VAT (20%)
        var amountTTC = course.PriceSnapshot;
        var amountHT = amountTTC / 1.20m;
        var vatAmount = amountTTC - amountHT;

        var invoice = new Invoice {
            CourseId = courseId,
            ParentId = course.Student.ParentId,
            Amount = amountTTC,
            AmountHT = amountHT,
            VatAmount = vatAmount,
            Commission = course.CommissionSnapshot,
            TeacherEarning = teacherEarning,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.PENDING
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return await GetBillingByIdAsync(invoice.Id);
    }

    public async Task<BillingDto> GetBillingByIdAsync(Guid billingId) {
        var invoice = await _context.Invoices
            .Include(i => i.Course).ThenInclude(c => c.Subject)
            .Include(i => i.Course).ThenInclude(c => c.Teacher)
            .Include(i => i.Parent).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(i => i.Id == billingId);

        if (invoice == null) {
            throw new Exception("Billing record not found");
        }

        return MapBillingToDto(invoice);
    }

    public async Task<IEnumerable<BillingDto>> GetBillingsByParentIdAsync(Guid parentId) {
        var invoices = await _context.Invoices
            .Include(i => i.Course).ThenInclude(c => c.Subject)
            .Include(i => i.Course).ThenInclude(c => c.Teacher)
            .Include(i => i.Parent).ThenInclude(p => p.User)
            .Where(i => i.ParentId == parentId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();

        return invoices.Select(MapBillingToDto);
    }

    public async Task<IEnumerable<BillingDto>> GetBillingsByTeacherIdAsync(Guid teacherId) {
        var invoices = await _context.Invoices
            .Include(i => i.Course).ThenInclude(c => c.Subject)
            .Include(i => i.Course).ThenInclude(c => c.Teacher)
            .Include(i => i.Parent).ThenInclude(p => p.User)
            .Where(i => i.Course.TeacherId == teacherId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();

        return invoices.Select(MapBillingToDto);
    }

    public async Task<IEnumerable<BillingDto>> GetAllBillingsAsync() {
        var invoices = await _context.Invoices
            .Include(i => i.Course).ThenInclude(c => c.Subject)
            .Include(i => i.Course).ThenInclude(c => c.Teacher)
            .Include(i => i.Parent).ThenInclude(p => p.User)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();

        return invoices.Select(MapBillingToDto);
    }

    public async Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto, Guid parentId) {
        var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
        if (invoice == null) {
            throw new Exception("Invoice not found");
        }

        if (invoice.ParentId != parentId) {
            throw new Exception("Unauthorized: Invoice does not belong to this parent");
        }

        if (invoice.Status == InvoiceStatus.PAID) {
            throw new Exception("Invoice already paid");
        }

        var payment = new Payment {
            InvoiceId = dto.InvoiceId,
            ParentId = parentId,
            Amount = invoice.Amount,
            Method = Enum.Parse<PaymentMethod>(dto.Method),
            Status = PaymentStatus.PENDING,
            StripePaymentIntentId = dto.StripePaymentIntentId
        };

        _context.Payments.Add(payment);

        // Simulate payment processing - in real app, integrate with Stripe
        payment.Status = PaymentStatus.SUCCEEDED;
        payment.ProcessedAt = DateTime.UtcNow;
        
        invoice.Status = InvoiceStatus.PAID;
        invoice.PaidAt = DateTime.UtcNow;
        invoice.PaymentIntentId = dto.StripePaymentIntentId;

        await _context.SaveChangesAsync();

        return MapPaymentToDto(payment);
    }

    public async Task<PaymentHistoryDto> GetParentPaymentHistoryAsync(Guid parentId) {
        var billings = await GetBillingsByParentIdAsync(parentId);
        var payments = await _context.Payments
            .Where(p => p.ParentId == parentId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var totalPaid = billings.Where(i => i.Status == "PAID").Sum(i => i.Amount);
        var totalPending = billings.Where(i => i.Status == "PENDING").Sum(i => i.Amount);

        return new PaymentHistoryDto {
            Billings = billings.ToList(),
            Payments = payments.Select(MapPaymentToDto).ToList(),
            TotalPaid = totalPaid,
            TotalPending = totalPending
        };
    }

    public async Task<PaymentHistoryDto> GetTeacherPaymentHistoryAsync(Guid teacherId) {
        var billings = await GetBillingsByTeacherIdAsync(teacherId);
        
        var totalPaid = billings.Where(i => i.Status == "PAID").Sum(i => i.TeacherEarning);
        var totalPending = billings.Where(i => i.Status == "PENDING").Sum(i => i.TeacherEarning);

        return new PaymentHistoryDto {
            Billings = billings.ToList(),
            Payments = new List<PaymentDto>(),
            TotalPaid = totalPaid,
            TotalPending = totalPending
        };
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid billingId) {
        var invoice = await _context.Invoices
            .Include(i => i.Course).ThenInclude(c => c.Subject)
            .Include(i => i.Course).ThenInclude(c => c.Teacher).ThenInclude(t => t.User)
            .Include(i => i.Parent).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(i => i.Id == billingId);

        if (invoice == null) {
            throw new Exception("Billing record not found");
        }

        return await _pdfService.GenerateInvoicePdfAsync(invoice);
    }

    public async Task<Guid?> GetCourseTeacherIdAsync(Guid courseId) {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.TeacherId)
            .FirstOrDefaultAsync();

        return course == Guid.Empty ? null : course;
    }

    private BillingDto MapBillingToDto(Invoice invoice) {
        return new BillingDto {
            Id = invoice.Id,
            CourseId = invoice.CourseId,
            CourseName = invoice.Course.Subject.Name,
            ParentId = invoice.ParentId,
            ParentName = $"{invoice.Parent.User.FirstName} {invoice.Parent.User.LastName}",
            TeacherId = invoice.Course.TeacherId,
            Amount = invoice.Amount,
            AmountHT = invoice.AmountHT,
            VatAmount = invoice.VatAmount,
            Commission = invoice.Commission,
            TeacherEarning = invoice.TeacherEarning,
            Status = invoice.Status.ToString(),
            IssuedAt = invoice.IssuedAt,
            PaidAt = invoice.PaidAt,
            InvoiceNumber = invoice.InvoiceNumber
        };
    }

    private PaymentDto MapPaymentToDto(Payment payment) {
        return new PaymentDto {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            ParentId = payment.ParentId,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            StripePaymentIntentId = payment.StripePaymentIntentId,
            ErrorMessage = payment.ErrorMessage,
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.ProcessedAt
        };
    }
}
