using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class Administrator
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string AccessLevel { get; set; } = "SUPPORT";
    public DateTime? LastActionAt { get; set; }

    public List<AdminLog> AdminLogs { get; set; } = new();
    public List<StaticPage> StaticPages { get; set; } = new();
}
