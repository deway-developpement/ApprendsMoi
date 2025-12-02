namespace backend.Domains.Users;

using backend.Database.Models;
using System.ComponentModel.DataAnnotations;

public class UserDto {
    public int Id { get; set; }
    public string? Username { get; set; }
    public ProfileType Profile { get; set; }
}

public class CreateUserRequest {
    [Required]
    public string? Username { get; set; }
    public ProfileType? Profile { get; set; }
}
