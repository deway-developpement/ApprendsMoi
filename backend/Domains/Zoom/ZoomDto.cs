using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Zoom;

public class CreateMeetingRequest
{
    public string? Topic { get; set; }

    [Required(ErrorMessage = "TeacherId is required")]
    public Guid? TeacherId { get; set; }

    [Required(ErrorMessage = "StudentId is required")]
    public Guid? StudentId { get; set; }

    [Required(ErrorMessage = "Time is required")]
    public DateTime? Time { get; set; }

    [Required(ErrorMessage = "Duration is required")]
    [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes (24 hours)")]
    public int? Duration { get; set; }
}

public class ZoomSignatureRequest
{
    public string MeetingNumber { get; set; } = string.Empty;
    public int Role { get; set; } = 0;
}

public class HostSignatureRequest
{
    public Guid UserId { get; set; }
}

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
