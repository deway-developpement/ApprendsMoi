namespace backend.Database.Models;

public class Student {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Username { get; set; } = string.Empty;
    public GradeLevel? GradeLevel { get; set; }
    public DateOnly? BirthDate { get; set; }
    
    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
