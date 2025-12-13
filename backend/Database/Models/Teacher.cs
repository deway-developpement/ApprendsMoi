using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class Teacher
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string? Bio { get; set; }
    public string VerificationStatus { get; set; } = "PENDING";
    public bool IsPremium { get; set; }
    public string? Location { get; set; }
    public int? TravelRadiusKm { get; set; }

    public List<TeacherSubject> TeacherSubjects { get; set; } = new();
    public List<TeacherDocument> Documents { get; set; } = new();
    public List<Availability> Availabilities { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Favorite> FavoritedBy { get; set; } = new();
    public List<Conversation> Conversations { get; set; } = new();
}
