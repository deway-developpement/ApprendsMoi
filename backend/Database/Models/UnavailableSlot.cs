namespace backend.Database.Models;

public class UnavailableSlot
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public DateTime BlockedDate { get; set; } // The specific date this slot is blocked
    public TimeOnly BlockedStartTime { get; set; }
    public TimeOnly BlockedEndTime { get; set; }
    public string? Reason { get; set; } // e.g., "Course booked", "Teacher meeting", etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
