namespace backend.Database.Models;

/// <summary>
/// Extension methods for User entity
/// </summary>
public static class UserExtensions {
    /// <summary>
    /// Get the full name of the user (FirstName + LastName)
    /// </summary>
    public static string GetFullName(this User user) {
        return $"{user.FirstName} {user.LastName}".Trim();
    }
}
