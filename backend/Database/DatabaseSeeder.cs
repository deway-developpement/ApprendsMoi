using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Database;

public static class DatabaseSeeder {
    private const string DEFAULT_PASSWORD = "Pass123";
    private const string ADMIN_EMAIL = "admin@test.com";

    public static async Task SeedAsync(AppDbContext context, bool populate = false) {
        // Create admin user
        await CreateAdminAsync(context);

        if (populate) {
            await CreateTestUsersAsync(context);
        }

        await context.SaveChangesAsync();
    }

    private static async Task CreateAdminAsync(AppDbContext context) {
        // Check if admin already exists
        var existingAdmin = await context.Administrators
            .FirstOrDefaultAsync(a => a.Email == ADMIN_EMAIL);

        if (existingAdmin == null) {
            var adminUser = new User {
                FirstName = "Admin",
                LastName = "System",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DEFAULT_PASSWORD),
                Profile = ProfileType.Admin,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            var administrator = new Administrator {
                User = adminUser,
                Email = ADMIN_EMAIL
            };

            context.Users.Add(adminUser);
            context.Administrators.Add(administrator);
            Console.WriteLine($"  ✓ Admin created: {ADMIN_EMAIL}");
        } else {
            Console.WriteLine($"  ℹ Admin exists: {ADMIN_EMAIL}");
        }
    }

    private static async Task CreateTestUsersAsync(AppDbContext context) {
        // Create 2 parents first
        var parent1 = await CreateParentIfNotExists(context, "parent1@test.com", "Parent", "One");
        var parent2 = await CreateParentIfNotExists(context, "parent2@test.com", "Parent", "Two");

        // Save parents to get their IDs
        await context.SaveChangesAsync();

        // Create 2 students for parent1
        if (parent1 != null) {
            await CreateStudentIfNotExists(context, "student1", "Student", "One", parent1.UserId);
            await CreateStudentIfNotExists(context, "student2", "Student", "Two", parent1.UserId);
        }

        // Create 2 teachers
        await CreateTeacherIfNotExists(context, "teacher1@test.com", "Teacher", "One");
        await CreateTeacherIfNotExists(context, "teacher2@test.com", "Teacher", "Two");

        await context.SaveChangesAsync();
        
        Console.WriteLine($"✓ Test users created:");
        Console.WriteLine($"  - Students: student1, student2)");
        Console.WriteLine($"  - Teachers: teacher1@test.com, teacher2@test.com");
        Console.WriteLine($"  - Parents: parent1@test.com, parent2@test.com");
    }

    private static async Task<Parent?> CreateParentIfNotExists(AppDbContext context, string email, string firstName, string lastName) {
        var existing = await context.Parents.FirstOrDefaultAsync(p => p.Email == email);
        if (existing != null) return existing;

        var user = new User {
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DEFAULT_PASSWORD),
            Profile = ProfileType.Parent,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var parent = new Parent {
            User = user,
            Email = email
        };

        context.Users.Add(user);
        context.Parents.Add(parent);
        return parent;
    }

    private static async Task CreateStudentIfNotExists(AppDbContext context, string username, string firstName, string lastName, Guid parentId) {
        var existing = await context.Students.FirstOrDefaultAsync(s => s.Username == username);
        if (existing != null) return;

        var user = new User {
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DEFAULT_PASSWORD),
            Profile = ProfileType.Student,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var student = new Student {
            User = user,
            Username = username,
            ParentId = parentId,
            GradeLevel = GradeLevel.SIXIEME
        };

        context.Users.Add(user);
        context.Students.Add(student);
    }

    private static async Task CreateTeacherIfNotExists(AppDbContext context, string email, string firstName, string lastName) {
        var existing = await context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
        if (existing != null) return;

        var user = new User {
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DEFAULT_PASSWORD),
            Profile = ProfileType.Teacher,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var teacher = new Teacher {
            User = user,
            Email = email,
            VerificationStatus = VerificationStatus.VERIFIED,
            City = "Paris"
        };

        context.Users.Add(user);
        context.Teachers.Add(teacher);
    }
}
