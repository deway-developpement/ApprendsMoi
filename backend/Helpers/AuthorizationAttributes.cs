using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using backend.Database.Models;
using System.Security.Claims;

namespace backend.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly ProfileType[] _allowedRoles;

    public RequireRoleAttribute(params ProfileType[] allowedRoles)
    {
        _allowedRoles = allowedRoles ?? throw new ArgumentNullException(nameof(allowedRoles));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Authentication required" });
            return;
        }

        // Get user role from claims
        var profileClaim = JwtHelper.GetUserProfileFromClaims(context.HttpContext.User)?.ToString();
        
        if (string.IsNullOrEmpty(profileClaim))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Parse the role
        if (!Enum.TryParse<ProfileType>(profileClaim, out var userProfile))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check if user has one of the allowed roles
        if (!_allowedRoles.Contains(userProfile))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

public class AdminOnlyAttribute : RequireRoleAttribute
{
    public AdminOnlyAttribute() : base(ProfileType.Admin) { }
}

public class TeacherOnlyAttribute : RequireRoleAttribute
{
    public TeacherOnlyAttribute() : base(ProfileType.Teacher) { }
}

public class TeacherOrAdminAttribute : RequireRoleAttribute
{
    public TeacherOrAdminAttribute() : base(ProfileType.Teacher, ProfileType.Admin) { }
}

public class ParentOrAdminAttribute : RequireRoleAttribute
{
    public ParentOrAdminAttribute() : base(ProfileType.Parent, ProfileType.Admin) { }
}

/// <summary>
/// Requires the user to be a verified teacher (VerificationStatus.VERIFIED or DIPLOMA_VERIFIED)
/// Accepts teachers who have completed identity verification OR diploma verification
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class VerifiedTeacherOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Authentication required" });
            return;
        }

        // Check if user is a teacher
        var profileClaim = JwtHelper.GetUserProfileFromClaims(context.HttpContext.User);
        if (profileClaim != ProfileType.Teacher)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Get teacher verification status from database
        var userId = JwtHelper.GetUserIdFromClaims(context.HttpContext.User);
        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid user ID" });
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<Database.AppDbContext>();
        var teacher = await dbContext.Teachers.FindAsync(userId.Value);
        
        if (teacher == null)
        {
            context.Result = new NotFoundObjectResult(new { error = "Teacher profile not found" });
            return;
        }

        if (teacher.VerificationStatus != VerificationStatus.VERIFIED && 
            teacher.VerificationStatus != VerificationStatus.DIPLOMA_VERIFIED)
        {
            context.Result = new ObjectResult(new 
            { 
                error = "Teacher account not verified",
                message = "Votre compte n'est pas encore vérifié. Veuillez soumettre vos documents d'identité et attendre l'approbation.",
                verificationStatus = teacher.VerificationStatus.ToString()
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }
    }
}
