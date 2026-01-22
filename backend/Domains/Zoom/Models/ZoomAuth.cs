using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Zoom.Models;

public class CreateMeetingRequest
{
    public string? Topic { get; set; }

    [Required]
    public Guid TeacherId { get; set; }

    [Required]
    public Guid StudentId { get; set; }
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
