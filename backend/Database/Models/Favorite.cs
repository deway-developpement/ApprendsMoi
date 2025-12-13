using System;

namespace backend.Database.Models;

public class Favorite
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
