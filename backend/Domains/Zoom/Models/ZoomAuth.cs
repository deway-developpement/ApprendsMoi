using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Zoom.Models;

public class CreateMeetingRequest
{
    public string? Topic { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "userId is required")]
    public int UserId { get; set; }
}

public class ZoomSignatureRequest
{
    public string MeetingNumber { get; set; } = string.Empty;
    public int Role { get; set; } = 0;
}

public class HostSignatureRequest
{
    public int UserId { get; set; }
}
