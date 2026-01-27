namespace backend.Domains.Ratings;

public class RatingDto {
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRatingDto {
    public Guid TeacherId { get; set; }
    public Guid? CourseId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class UpdateRatingDto {
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class AnonymousRatingDto {
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class TeacherRatingStatsDto {
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public decimal? AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public List<AnonymousRatingDto> RecentRatings { get; set; } = new();
}
