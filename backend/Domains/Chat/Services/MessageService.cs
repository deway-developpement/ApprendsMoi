using backend.Database;
using backend.Database.Models;
using backend.Domains.Chat.Mappers;
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
                .ThenInclude(a => a.Uploader)
            .OrderBy(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        var messageDtos = messages.ToDtos();

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
        
        // Verify chat exists and sender is a participant
        var chat = await _db.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);
        
        if (chat == null) {
            throw new InvalidOperationException("Chat not found");
        }

        // Verify sender is a participant in the chat
        if (chat.TeacherId != senderId && chat.ParentId != senderId && chat.StudentId != senderId) {
            throw new UnauthorizedAccessException("Sender is not a participant in this chat");
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
            SenderName = sender != null ? sender.GetFullName() : "Unknown",
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
                .ThenInclude(a => a.Uploader)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return messages.ToDtos();
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
                .ThenInclude(a => a.Uploader)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);

        var reversedMessages = messages.AsEnumerable().Reverse();
        return reversedMessages.ToDtos();
    }

    /// <summary>
    /// Check if user is participant in chat (including read-only access for parents viewing children's chats)
    /// </summary>
    public async Task<bool> UserIsParticipantInChatAsync(
        Guid chatId,
        Guid userId,
        CancellationToken ct = default) {
        
        var chat = await _db.Chats
            .Include(c => c.Student)
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);

        if (chat == null) return false;

        // Check if user is direct participant
        if (chat.TeacherId == userId || chat.ParentId == userId || chat.StudentId == userId) {
            return true;
        }

        // Check if user is a parent viewing their child's chat
        if (chat.ChatType == ChatType.StudentChat && chat.StudentId.HasValue) {
            var isParentOfStudent = await _db.Students
                .AnyAsync(s => s.UserId == chat.StudentId.Value && s.ParentId == userId, ct);
            return isParentOfStudent;
        }

        return false;
    }

    /// <summary>
    /// Check if user can send messages in chat (not read-only)
    /// </summary>
    public async Task<bool> UserCanSendMessageAsync(
        Guid chatId,
        Guid userId,
        CancellationToken ct = default) {
        
        var chat = await _db.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);

        if (chat == null) return false;

        // Only direct participants can send messages
        return chat.TeacherId == userId || chat.ParentId == userId || chat.StudentId == userId;
    }
}
