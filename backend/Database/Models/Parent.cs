using System;
using System.Collections.Generic;

namespace backend.Database.Models;

public class Parent
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? AddressJson { get; set; }

    public List<Student> Students { get; set; } = new();
    public List<Conversation> Conversations { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}
