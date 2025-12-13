using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class Student
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;

    public string GradeLevel { get; set; } = null!;
    public DateTime BirthDate { get; set; }

    public List<Course> Courses { get; set; } = new();
    public List<Conversation> Conversations { get; set; } = new();
}
