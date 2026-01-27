using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Chat;

public class MessageService(AppDbContext db) {
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Get messages for a chat with pagination
    /// </summary>
    public async Task<PaginatedMessagesDto> GetMessagesByChatAsync(
        Guid chatId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default) {
        
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var totalCount = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .CountAsync(ct);

        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .OrderBy(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        var messageDtos = messages.Select(m => new MessageDto {
            MessageId = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
            SenderProfilePicture = m.Sender.ProfilePicture,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            Attachments = m.Attachments.Select(a => new ChatAttachmentDto {
                AttachmentId = a.Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                FileSize = a.FileSize,
                FileType = a.FileType,
                UploadedBy = a.UploadedBy,
                CreatedAt = a.CreatedAt
            }).ToList()
        }).ToList();

        return new PaginatedMessagesDto {
            Messages = messageDtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Create a new message
    /// </summary>
    public async Task<MessageDto> CreateMessageAsync(
        Guid chatId,
        Guid senderId,
        CreateMessageDto dto,
        CancellationToken ct = default) {
        
        // Verify sender is part of chat
        var chat = await _db.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);
        
        if (chat == null) {
            throw new InvalidOperationException("Chat not found");
        }

        var message = new Database.Models.Message {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        
        // Update chat's UpdatedAt timestamp
        chat.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Load sender info
        var sender = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == senderId, ct);

        return new MessageDto {
            MessageId = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown",
            SenderProfilePicture = sender?.ProfilePicture,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            Attachments = []
        };
    }

    /// <summary>
    /// Search messages in a chat by content
    /// </summary>
    public async Task<List<MessageDto>> SearchMessagesAsync(
        Guid chatId,
        string searchTerm,
        CancellationToken ct = default) {
        
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId && m.Content.Contains(searchTerm))
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return messages.Select(m => new MessageDto {
            MessageId = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
            SenderProfilePicture = m.Sender.ProfilePicture,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            Attachments = m.Attachments.Select(a => new ChatAttachmentDto {
                AttachmentId = a.Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                FileSize = a.FileSize,
                FileType = a.FileType,
                UploadedBy = a.UploadedBy,
                CreatedAt = a.CreatedAt
            }).ToList()
        }).ToList();
    }

    /// <summary>
    /// Get recent messages (for real-time updates)
    /// </summary>
    public async Task<List<MessageDto>> GetRecentMessagesAsync(
        Guid chatId,
        int limit = 50,
        CancellationToken ct = default) {
        
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);

        var reversedMessages = messages.AsEnumerable().Reverse();
        return reversedMessages.Select(m => new MessageDto {
            MessageId = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
            SenderProfilePicture = m.Sender.ProfilePicture,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            Attachments = m.Attachments.Select(a => new ChatAttachmentDto {
                AttachmentId = a.Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                FileSize = a.FileSize,
                FileType = a.FileType,
                UploadedBy = a.UploadedBy,
                CreatedAt = a.CreatedAt
            }).ToList()
        }).ToList();
    }

    /// <summary>
    /// Check if user is participant in chat
    /// </summary>
    public async Task<bool> UserIsParticipantInChatAsync(
        Guid chatId,
        Guid userId,
        CancellationToken ct = default) {
        
        var chat = await _db.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);

        if (chat == null) return false;

        return chat.TeacherId == userId || chat.ParentId == userId || chat.StudentId == userId;
    }
}
