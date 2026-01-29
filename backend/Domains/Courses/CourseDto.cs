namespace backend.Domains.Courses;

public class CourseDto {
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PriceSnapshot { get; set; }
    public decimal CommissionSnapshot { get; set; }
    public string? MeetingLink { get; set; }
    public bool StudentAttended { get; set; }
    public DateTime? AttendanceMarkedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCourseDto {
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public string Format { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int DurationMinutes { get; set; }
}

public class UpdateCourseDto {
    public DateTime? StartDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Status { get; set; }
    public string? MeetingLink { get; set; }
}

public class MarkAttendanceDto {
    public bool Attended { get; set; }
}
