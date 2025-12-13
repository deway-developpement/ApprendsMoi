using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public List<Message> Messages { get; set; } = new();
}
