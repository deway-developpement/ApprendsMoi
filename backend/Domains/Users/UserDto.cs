namespace backend.Domains.Users;

using backend.Database.Models;
using System.ComponentModel.DataAnnotations;

public class UserDto {
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public ProfileType Profile { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminDto : UserDto {
    // Admin-specific fields can be added here if needed
}

public class TeacherDto : UserDto {
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public bool IsPremium { get; set; }
    public string? City { get; set; }
    public int? TravelRadiusKm { get; set; }
}

public class ParentDto : UserDto {
    public string? PhoneNumber { get; set; }
    public string? StripeCustomerId { get; set; }
}

public class StudentDto : UserDto {
    public GradeLevel? GradeLevel { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Guid ParentId { get; set; }
}

public class CreateUserRequest {
    [Required]
    public string? Username { get; set; }
    public ProfileType? Profile { get; set; }
}
