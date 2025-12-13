using System;

namespace backend.Database.Models;

public class StaticPage
{
    public string Slug { get; set; } = null!;
    public Guid LastEditedBy { get; set; }
    public Administrator LastEditor { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime LastUpdatedAt { get; set; }
}
