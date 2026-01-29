namespace backend.Database.Models;

public class Course {
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public CourseStatus Status { get; set; } = CourseStatus.PENDING;
    public CourseFormat Format { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PriceSnapshot { get; set; }
    public decimal CommissionSnapshot { get; set; }
    public string? MeetingLink { get; set; }
    public DateTime? TeacherValidationAt { get; set; }
    public DateTime? ParentValidationAt { get; set; }
    public bool StudentAttended { get; set; } = false;
    public DateTime? AttendanceMarkedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
