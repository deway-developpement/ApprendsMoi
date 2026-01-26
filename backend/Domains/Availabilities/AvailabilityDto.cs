using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Availabilities;

public class CreateAvailabilityRequest
{
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday)")]
    public int? DayOfWeek { get; set; }

    // Optional: if not specified for non-recurring slots, defaults to the next future day with this day of week
    public DateOnly? AvailabilityDate { get; set; }

    [Required(ErrorMessage = "StartTime is required")]
    public string? StartTime { get; set; }

    [Required(ErrorMessage = "EndTime is required")]
    public string? EndTime { get; set; }

    public bool IsRecurring { get; set; } = true;
}

public class BlockAvailabilityRequest
{
    [Required(ErrorMessage = "BlockedDate is required")]
    public DateTime? BlockedDate { get; set; }

    [Required(ErrorMessage = "BlockedStartTime is required")]
    public string? BlockedStartTime { get; set; }

    [Required(ErrorMessage = "BlockedEndTime is required")]
    public string? BlockedEndTime { get; set; }

    public string? Reason { get; set; }
}

public class AvailabilityResponse
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public DateOnly? AvailabilityDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsRecurring { get; set; }
}

public class UnavailableSlotResponse
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime BlockedDate { get; set; }
    public TimeOnly BlockedStartTime { get; set; }
    public TimeOnly BlockedEndTime { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
