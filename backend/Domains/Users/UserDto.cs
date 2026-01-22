namespace backend.Domains.Users;

using backend.Database.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

public class AdminDto : UserDto { }

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
    // Students use username instead of email - prevent email from appearing in JSON/Swagger
    [JsonIgnore]
    public new string? Email { get => null; set { } }
    
    public GradeLevel? GradeLevel { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Guid ParentId { get; set; }
}

public class CreateUserRequest {
    [Required]
    public string? Username { get; set; }
    public ProfileType? Profile { get; set; }
}
