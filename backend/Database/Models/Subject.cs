using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class Subject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public List<TeacherSubject> TeacherSubjects { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
}
