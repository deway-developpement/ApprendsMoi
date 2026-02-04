namespace backend.Domains.Stats;

public class AdminStatsDto {
    public int ActiveUsersThisMonth { get; set; }
    public decimal CommissionsThisMonth { get; set; }
    public int CompletedCoursesThisMonth { get; set; }
}

public class TeacherStatsDto {
    public decimal? AverageRating { get; set; }
    public int NumberOfReviewers { get; set; }
    public decimal EarningsThisMonth { get; set; }
    public int CurrentStudentsFollowing { get; set; }
}

public class ParentStatsDto {
    public decimal TotalDebt { get; set; }
    public int CoursesBookedThisMonth { get; set; }
    public int NumberOfChildren { get; set; }
}

public class StudentStatsDto {
    public decimal TotalHoursThisMonth { get; set; }
    public int NumberOfCoursesThisMonth { get; set; }
}

// Generic response wrapper that can hold any of the above
public class StatsResponseDto {
    public string UserType { get; set; } = string.Empty;
    public object Stats { get; set; } = null!;
}
