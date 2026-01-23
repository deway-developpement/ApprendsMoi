namespace backend.Database.Models;

public class TeacherSubject {
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    
    public GradeLevel? LevelMin { get; set; }
    public GradeLevel? LevelMax { get; set; }
    public decimal PricePerHour { get; set; }
}
