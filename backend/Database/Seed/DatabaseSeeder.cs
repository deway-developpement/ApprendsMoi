using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Database.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Username == "admin"))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = "hashed-admin-password",
                FirstName = "Super",
                LastName = "Admin",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                Profile = ProfileType.Admin,
                Administrator = new Administrator
                {
                    UserId = Guid.Empty,
                    AccessLevel = "SUPER_ADMIN",
                    LastActionAt = DateTime.UtcNow
                }
            };

            db.Users.Add(adminUser);

            adminUser.Administrator.UserId = adminUser.Id;
        }

        if (!await db.Users.AnyAsync(u => u.Username == "parent1"))
        {
            var parentUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "parent1",
                PasswordHash = "hashed-parent-password",
                FirstName = "Alice",
                LastName = "Martin",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                Profile = ProfileType.Parent,
                Parent = new Parent
                {
                    UserId = Guid.Empty,
                    Email = "alice@example.com",
                    Phone = "+33601010101",
                    StripeCustomerId = "cus_test_123",
                    AddressJson = "{\"city\": \"Paris\"}"
                }
            };

            db.Users.Add(parentUser);

            parentUser.Parent.UserId = parentUser.Id;

            var studentUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "student1",
                PasswordHash = "hashed-student-password",
                FirstName = "Emma",
                LastName = "Martin",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                Profile = ProfileType.Student
            };

            db.Users.Add(studentUser);

            var child = new Student
            {
                UserId = studentUser.Id,
                ParentId = parentUser.Id,
                GradeLevel = "CE1",
                BirthDate = new DateTime(2017, 4, 12, 0, 0, 0, DateTimeKind.Utc)
            };

            db.Students.Add(child);
        }

        await db.SaveChangesAsync();
    }
}
