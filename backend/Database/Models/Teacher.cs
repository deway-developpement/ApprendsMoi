namespace backend.Database.Models;

public class Teacher {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.PENDING;
    public bool IsPremium { get; set; } = false;
    public string? City { get; set; }
    public int? TravelRadiusKm { get; set; }
    
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
