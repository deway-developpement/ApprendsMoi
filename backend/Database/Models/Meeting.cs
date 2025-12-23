namespace backend.Database.Models;

public class Meeting
{
    public int Id { get; set; }
    public long ZoomMeetingId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public string StartUrl { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public int Duration { get; set; } // in minutes
}
