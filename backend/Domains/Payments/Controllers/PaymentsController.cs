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
    [RequireRole(ProfileType.Admin, ProfileType.Parent, ProfileType.Teacher)]
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
    [RequireRole(ProfileType.Admin, ProfileType.Parent, ProfileType.Teacher)]
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

    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<BillingDto>>> GetBillingsByUser([FromQuery] Guid? targetUserId = null) {
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

    [HttpGet("history")]
    [RequireRole(ProfileType.Parent, ProfileType.Teacher, ProfileType.Admin)]
    public async Task<ActionResult<PaymentHistoryDto>> GetPaymentHistory([FromQuery] Guid? targetUserId = null) {
        try {
            var userId = JwtHelper.GetUserIdFromClaims(User);
            var userProfile = JwtHelper.GetUserProfileFromClaims(User);

            if (userId == null || userProfile == null) {
                return Unauthorized();
            }

            // Determine which user's history to fetch
            var queryUserId = targetUserId ?? userId.Value;

            // Non-admins can only see their own history
            if (userProfile != ProfileType.Admin && queryUserId != userId) {
                return Forbid();
            }

            // If querying own history, use their profile type
            if (queryUserId == userId) {
                return userProfile switch {
                    ProfileType.Parent => Ok(await _paymentService.GetParentPaymentHistoryAsync(queryUserId)),
                    ProfileType.Teacher => Ok(await _paymentService.GetTeacherPaymentHistoryAsync(queryUserId)),
                    ProfileType.Admin => BadRequest(new { message = "Admins must specify a user ID" }),
                    _ => BadRequest(new { message = "Invalid user profile type" })
                };
            }

            // Admin querying another user - try both parent and teacher
            var parentHistory = await _paymentService.GetParentPaymentHistoryAsync(queryUserId);
            if (parentHistory.Billings.Any() || parentHistory.Payments.Any()) {
                return Ok(parentHistory);
            }

            var teacherHistory = await _paymentService.GetTeacherPaymentHistoryAsync(queryUserId);
            return Ok(teacherHistory);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
