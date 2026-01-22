namespace backend.Domains.Zoom.Models;

public class MeetingResponse
{
    public int Id { get; set; }
    public long MeetingId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public int Duration { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
}

public class CreateMeetingResponse
{
    public int Id { get; set; }
    public long MeetingId { get; set; }
    public string MeetingNumber { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public string StartUrl { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ParticipantSignature { get; set; } = string.Empty;
    public string SdkKey { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
}

public class MeetingDetailsResponse
{
    public int Id { get; set; }
    public long MeetingId { get; set; }
    public string MeetingNumber { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public string StartUrl { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public int Duration { get; set; }
    public string ParticipantSignature { get; set; } = string.Empty;
    public string SdkKey { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
}

public class SignatureResponse
{
    public string Signature { get; set; } = string.Empty;
    public string SdkKey { get; set; } = string.Empty;
}
