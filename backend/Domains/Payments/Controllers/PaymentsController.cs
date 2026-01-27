using backend.Domains.Payments;
using backend.Domains.Payments.Services;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Database.Models;

namespace backend.Domains.Payments.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase {
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) {
        _paymentService = paymentService;
    }

    [HttpPost("invoices/course/{courseId}")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<InvoiceDto>> CreateInvoiceForCourse(Guid courseId) {
        try {
            var invoice = await _paymentService.CreateInvoiceForCourseAsync(courseId);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invoices/{id}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid id) {
        try {
            var invoice = await _paymentService.GetInvoiceByIdAsync(id);
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Check authorization
            if (userProfile != ProfileType.Admin.ToString() && invoice.ParentId != userId) {
                return Forbid();
            }

            return Ok(invoice);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("invoices")]
    [RequireRole(ProfileType.Admin)]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAllInvoices() {
        try {
            var invoices = await _paymentService.GetAllInvoicesAsync();
            return Ok(invoices);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invoices/parent/{parentId}")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoicesByParent(Guid parentId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Parents can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && parentId != userId) {
                return Forbid();
            }

            var invoices = await _paymentService.GetInvoicesByParentIdAsync(parentId);
            return Ok(invoices);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invoices/teacher/{teacherId}")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoicesByTeacher(Guid teacherId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var invoices = await _paymentService.GetInvoicesByTeacherIdAsync(teacherId);
            return Ok(invoices);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("process")]
    [RequireRole(ProfileType.Parent, ProfileType.Admin)]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] CreatePaymentDto dto) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var payment = await _paymentService.ProcessPaymentAsync(dto, userId);
            return Ok(payment);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history/parent/{parentId}")]
    public async Task<ActionResult<PaymentHistoryDto>> GetParentPaymentHistory(Guid parentId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Parents can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && parentId != userId) {
                return Forbid();
            }

            var history = await _paymentService.GetParentPaymentHistoryAsync(parentId);
            return Ok(history);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history/teacher/{teacherId}")]
    public async Task<ActionResult<PaymentHistoryDto>> GetTeacherPaymentHistory(Guid teacherId) {
        try {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = User.FindFirst("profile")?.Value;

            // Teachers can only see their own, admins can see all
            if (userProfile != ProfileType.Admin.ToString() && teacherId != userId) {
                return Forbid();
            }

            var history = await _paymentService.GetTeacherPaymentHistoryAsync(teacherId);
            return Ok(history);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
