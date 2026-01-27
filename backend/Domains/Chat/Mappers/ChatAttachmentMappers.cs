using backend.Database.Models;

namespace backend.Domains.Chat.Mappers;

/// <summary>
/// Extension methods for mapping ChatAttachment entities to DTOs
/// </summary>
public static class ChatAttachmentMappers {
    /// <summary>
    /// Map a ChatAttachment entity to ChatAttachmentDto
    /// </summary>
    public static ChatAttachmentDto ToDto(this ChatAttachment attachment) {
        return new ChatAttachmentDto {
            AttachmentId = attachment.Id,
            FileName = attachment.FileName,
            FileUrl = attachment.FileUrl,
            FileSize = attachment.FileSize,
            FileType = attachment.FileType,
            UploadedBy = attachment.UploadedBy,
            UploadedByName = attachment.Uploader != null ? attachment.Uploader.GetFullName() : "Unknown",
            CreatedAt = attachment.CreatedAt
        };
    }

    /// <summary>
    /// Map a collection of ChatAttachment entities to ChatAttachmentDto list
    /// </summary>
    public static List<ChatAttachmentDto> ToDtos(this IEnumerable<ChatAttachment> attachments) {
        return attachments.Select(a => a.ToDto()).ToList();
    }
}
