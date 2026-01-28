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

    [HttpPost("course/{courseId}")]
    [RequireRole(ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<BillingDto>> CreateBillingForCourse(Guid courseId) {
        try {
            var billing = await _paymentService.CreateBillingForCourseAsync(courseId);
            return CreatedAtAction(nameof(GetBilling), new { id = billing.Id }, billing);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BillingDto>> GetBilling(Guid id) {
        try {
            var billing = await _paymentService.GetBillingByIdAsync(id);
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Check authorization
            if (userProfile != ProfileType.Admin && billing.ParentId != userId) {
                return Forbid();
            }

            return Ok(billing);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("invoice/{id}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id) {
        try {
            var billing = await _paymentService.GetBillingByIdAsync(id);
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Check authorization
            if (userProfile != ProfileType.Admin && billing.ParentId != userId) {
                return Forbid();
            }

            var pdfBytes = await _paymentService.GenerateInvoicePdfAsync(id);
            var fileName = $"Facture_{billing.InvoiceNumber?.Replace("/", "-")}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("")]
    [AdminOnly]
    public async Task<ActionResult<IEnumerable<BillingDto>>> GetAllBillings() {
        try {
            var billings = await _paymentService.GetAllBillingsAsync();
            return Ok(billings);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("user/{targetUserId?}")]
    public async Task<ActionResult<IEnumerable<BillingDto>>> GetBillingsByUser(Guid? targetUserId = null) {
        try {
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Determine which user's billings to fetch
            var queryUserId = targetUserId ?? userId.Value;

            // Non-admins can only see their own billings
            if (userProfile != ProfileType.Admin && queryUserId != userId) {
                return Forbid();
            }

            // If querying own billings, use their profile type
            if (queryUserId == userId) {
                return userProfile switch {
                    ProfileType.Parent => Ok(await _paymentService.GetBillingsByParentIdAsync(queryUserId)),
                    ProfileType.Teacher => Ok(await _paymentService.GetBillingsByTeacherIdAsync(queryUserId)),
                    ProfileType.Admin => Ok(await _paymentService.GetAllBillingsAsync()),
                    _ => BadRequest(new { message = "Invalid user profile type" })
                };
            }

            // Admin querying another user - try both parent and teacher
            var parentBillings = await _paymentService.GetBillingsByParentIdAsync(queryUserId);
            if (parentBillings.Any()) {
                return Ok(parentBillings);
            }

            var teacherBillings = await _paymentService.GetBillingsByTeacherIdAsync(queryUserId);
            return Ok(teacherBillings);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("process")]
    [RequireRole(ProfileType.Parent, ProfileType.Admin)]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] CreatePaymentDto dto) {
        try {
            var userId = JwtHelper.GetUserIdFromClaims(User);
            if (userId == null) {
                return Unauthorized();
            }
            
            var payment = await _paymentService.ProcessPaymentAsync(dto, userId.Value);
            return Ok(payment);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history/parent/{parentId}")]
    public async Task<ActionResult<PaymentHistoryDto>> GetParentPaymentHistory(Guid parentId) {
        try {
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Parents can only see their own, admins can see all
            if (userProfile != ProfileType.Admin && parentId != userId) {
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
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Teachers can only see their own, admins can see all
            if (userProfile != ProfileType.Admin && teacherId != userId) {
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
