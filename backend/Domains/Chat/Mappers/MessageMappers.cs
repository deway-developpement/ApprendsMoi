using backend.Database.Models;

namespace backend.Domains.Chat.Mappers;

/// <summary>
/// Extension methods for mapping Message entities to DTOs
/// </summary>
public static class MessageMappers {
    /// <summary>
    /// Map a Message entity to MessageDto
    /// </summary>
    public static MessageDto ToDto(this Message message) {
        return new MessageDto {
            MessageId = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = message.Sender.GetFullName(),
            SenderProfilePicture = message.Sender.ProfilePicture,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            Attachments = message.Attachments.ToDtos()
        };
    }

    /// <summary>
    /// Map a collection of Message entities to MessageDto list
    /// </summary>
    public static List<MessageDto> ToDtos(this IEnumerable<Message> messages) {
        return messages.Select(m => m.ToDto()).ToList();
    }
}
