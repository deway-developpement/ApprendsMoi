using System;

namespace backend.Database.Models;

public class TeacherSubject
{
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public string LevelMin { get; set; } = null!;
    public string LevelMax { get; set; } = null!;
    public decimal PricePerHour { get; set; }
}
