namespace backend.Domains.Chat;

using backend.Database.Models;
using System.ComponentModel.DataAnnotations;

public class ChatDto {
    public Guid ChatId { get; set; }
    public ChatType ChatType { get; set; }
    public Guid TeacherId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? StudentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? ParticipantProfilePicture { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsReadOnly { get; set; } = false;
}

public class ChatDetailDto : ChatDto {
    public List<MessageDto> Messages { get; set; } = [];
}

public class CreateChatDto {
    [Required]
    public ChatType ChatType { get; set; }
    
    // For parent-initiated chats: parent specifies the teacher they want to chat with
    public Guid? TeacherId { get; set; }
    
    // For teacher-initiated chats with a specific parent
    public Guid? ParentId { get; set; }
    
    // For chats related to student courses (set when course is booked)
    public Guid? StudentId { get; set; }
}

public class MessageDto {
    public Guid MessageId { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ChatAttachmentDto> Attachments { get; set; } = [];
}

public class CreateMessageDto {
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatAttachmentDto {
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public Guid UploadedBy { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PaginatedMessagesDto {
    public List<MessageDto> Messages { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
