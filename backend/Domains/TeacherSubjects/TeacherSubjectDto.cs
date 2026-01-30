namespace backend.Domains.TeacherSubjects;

public class TeacherSubjectDto {
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public decimal PricePerHour { get; set; }
}

public class CreateTeacherSubjectDto {
    public Guid SubjectId { get; set; }
    public decimal PricePerHour { get; set; }
}

public class UpdateTeacherSubjectDto {
    public decimal? PricePerHour { get; set; }
}
