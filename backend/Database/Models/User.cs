using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class User
{
    public Guid Id { get; set; }
    public List<Report> Reports { get; set; } = new();
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ProfileType Profile { get; set; }

    public Parent? Parent { get; set; }
    public Teacher? Teacher { get; set; }
    public Administrator? Administrator { get; set; }
    public Student? Student { get; set; }

    public List<Message> MessagesSent { get; set; } = new();
    public List<AdminLog> AdminLogs { get; set; } = new();
}
