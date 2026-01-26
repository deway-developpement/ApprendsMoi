using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Zoom.Models;

public class CreateMeetingRequest
{
    public string? Topic { get; set; }

    [Required(ErrorMessage = "TeacherId is required")]
    public Guid? TeacherId { get; set; }

    [Required(ErrorMessage = "StudentId is required")]
    public Guid? StudentId { get; set; }

    [Required(ErrorMessage = "Time is required")]
    public DateTime? Time { get; set; }
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
