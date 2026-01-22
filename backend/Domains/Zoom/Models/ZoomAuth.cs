using System.ComponentModel.DataAnnotations;

namespace backend.Domains.Zoom.Models;

public class CreateMeetingRequest
{
    public string? Topic { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "teacherId is required")]
    public int TeacherId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "studentId is required")]
    public int StudentId { get; set; }
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
