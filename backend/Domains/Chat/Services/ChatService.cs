using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Chat;

public class ChatService(AppDbContext db) {
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Get all chats for a teacher
    /// </summary>
    public async Task<List<ChatDto>> GetChatsByTeacherAsync(Guid teacherId, CancellationToken ct = default) {
        var chats = await _db.Chats
            .Where(c => c.TeacherId == teacherId && c.IsActive)
            .Include(c => c.Parent)
            .Include(c => c.Student)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

        var chatDtos = new List<ChatDto>();
        foreach (var chat in chats) {
            chatDtos.Add(MapChatToDto(chat));
        }

        return chatDtos;
    }

    /// <summary>
    /// Get all chats for a parent (parent chats only)
    /// </summary>
    public async Task<List<ChatDto>> GetChatsByParentAsync(Guid parentId, CancellationToken ct = default) {
        var chats = await _db.Chats
            .Where(c => c.ParentId == parentId && c.ChatType == ChatType.ParentChat && c.IsActive)
            .Include(c => c.Teacher)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

        var chatDtos = new List<ChatDto>();
        foreach (var chat in chats) {
            chatDtos.Add(MapChatToDto(chat));
        }

        return chatDtos;
    }

    /// <summary>
    /// Get all chats for a student (student chats only)
    /// </summary>
    public async Task<List<ChatDto>> GetChatsByStudentAsync(Guid studentId, CancellationToken ct = default) {
        var chats = await _db.Chats
            .Where(c => c.StudentId == studentId && c.ChatType == ChatType.StudentChat && c.IsActive)
            .Include(c => c.Teacher)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

        var chatDtos = new List<ChatDto>();
        foreach (var chat in chats) {
            chatDtos.Add(MapChatToDto(chat));
        }

        return chatDtos;
    }

    /// <summary>
    /// Get a specific chat by ID with all messages
    /// </summary>
    public async Task<ChatDetailDto?> GetChatByIdAsync(Guid chatId, CancellationToken ct = default) {
        var chat = await _db.Chats
            .Where(c => c.Id == chatId)
            .Include(c => c.Teacher)
            .Include(c => c.Parent)
            .Include(c => c.Student)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Attachments)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (chat == null) return null;

        var chatDto = MapChatToDto(chat);
        var detailDto = new ChatDetailDto {
            ChatId = chatDto.ChatId,
            ChatType = chatDto.ChatType,
            TeacherId = chatDto.TeacherId,
            ParentId = chatDto.ParentId,
            StudentId = chatDto.StudentId,
            ParticipantName = chatDto.ParticipantName,
            ParticipantProfilePicture = chatDto.ParticipantProfilePicture,
            LastMessage = chatDto.LastMessage,
            LastMessageTime = chatDto.LastMessageTime,
            UnreadCount = chatDto.UnreadCount,
            CreatedAt = chatDto.CreatedAt,
            UpdatedAt = chatDto.UpdatedAt,
            IsActive = chatDto.IsActive,
            Messages = chat.Messages.Select(m => new MessageDto {
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
            }).ToList()
        };

        return detailDto;
    }

    /// <summary>
    /// Create a new chat or return existing one
    /// Supports multiple scenarios:
    /// 1. Parent initiating chat with teacher (ChatType=ParentChat, TeacherId provided, called by Parent)
    /// 2. Teacher creating chat with parent (ChatType=ParentChat, ParentId provided, called by Teacher)
    /// 3. Auto-creation when course is booked (ChatType=StudentChat, StudentId+TeacherId provided)
    /// </summary>
    public async Task<ChatDto> CreateChatAsync(CreateChatDto dto, Guid userId, ProfileType userProfile, CancellationToken ct = default) {
        Guid teacherId = Guid.Empty;
        Guid? parentId = null;
        Guid? studentId = null;

        // Determine the participants based on user role and provided data
        if (dto.ChatType == ChatType.ParentChat) {
            if (userProfile == ProfileType.Parent) {
                // Parent initiating chat with teacher
                if (!dto.TeacherId.HasValue) {
                    throw new InvalidOperationException("TeacherId is required for parent-initiated chats");
                }
                teacherId = dto.TeacherId.Value;
                parentId = userId; // Current user is the parent
            } else if (userProfile == ProfileType.Teacher) {
                // Teacher creating chat with parent
                if (!dto.ParentId.HasValue) {
                    throw new InvalidOperationException("ParentId is required for teacher-created parent chats");
                }
                teacherId = userId; // Current user is the teacher
                parentId = dto.ParentId;
            } else {
                throw new InvalidOperationException("Only teachers and parents can create chats");
            }
        } else if (dto.ChatType == ChatType.StudentChat) {
            // Course-related chat (auto-created or manually created)
            if (!dto.TeacherId.HasValue || !dto.StudentId.HasValue) {
                throw new InvalidOperationException("TeacherId and StudentId are required for student chats");
            }
            teacherId = dto.TeacherId.Value;
            studentId = dto.StudentId;

            // Verify that the current user has permission (is the teacher or the student)
            if (userProfile != ProfileType.Admin && userId != teacherId && userId != studentId) {
                throw new InvalidOperationException("You don't have permission to create this chat");
            }
        }

        // Check for existing chat
        Database.Models.Chat? existingChat = null;
        
        if (dto.ChatType == ChatType.ParentChat && parentId.HasValue) {
            existingChat = await _db.Chats
                .FirstOrDefaultAsync(c => 
                    c.TeacherId == teacherId && 
                    c.ParentId == parentId &&
                    c.ChatType == ChatType.ParentChat,
                ct);
        } else if (dto.ChatType == ChatType.StudentChat && studentId.HasValue) {
            existingChat = await _db.Chats
                .FirstOrDefaultAsync(c => 
                    c.TeacherId == teacherId && 
                    c.StudentId == studentId &&
                    c.ChatType == ChatType.StudentChat,
                ct);
        }

        if (existingChat is not null) {
            existingChat.IsActive = true;
            await _db.SaveChangesAsync(ct);
            return MapChatToDto(existingChat);
        }

        // Create new chat
        var chat = new Database.Models.Chat {
            Id = Guid.NewGuid(),
            ChatType = dto.ChatType,
            TeacherId = teacherId,
            ParentId = parentId,
            StudentId = studentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync(ct);

        return MapChatToDto(chat);
    }

    /// <summary>
    /// Archive a chat (soft delete)
    /// </summary>
    public async Task<bool> ArchiveChatAsync(Guid chatId, CancellationToken ct = default) {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatId, ct);
        if (chat == null) return false;

        chat.IsActive = false;
        chat.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Reactivate an archived chat
    /// </summary>
    public async Task<bool> ReactivateChatAsync(Guid chatId, CancellationToken ct = default) {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatId, ct);
        if (chat == null) return false;

        chat.IsActive = true;
        chat.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Check if user has access to chat
    /// </summary>
    public async Task<bool> UserHasAccessToChatAsync(Guid chatId, Guid userId, CancellationToken ct = default) {
        var chat = await _db.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);

        if (chat == null) return false;

        // Check if user is the teacher, parent, or student in the chat
        return chat.TeacherId == userId || chat.ParentId == userId || chat.StudentId == userId;
    }

    /// <summary>
    /// Auto-create a student chat when a course is booked
    /// Called from the Course service when a course is created/confirmed
    /// </summary>
    public async Task<ChatDto> AutoCreateStudentChatAsync(Guid teacherId, Guid studentId, CancellationToken ct = default) {
        // Check if chat already exists
        var existingChat = await _db.Chats
            .FirstOrDefaultAsync(c => 
                c.TeacherId == teacherId && 
                c.StudentId == studentId &&
                c.ChatType == ChatType.StudentChat,
                ct);

        if (existingChat is not null) {
            existingChat.IsActive = true;
            await _db.SaveChangesAsync(ct);
            return MapChatToDto(existingChat);
        }

        // Create new chat
        var chat = new Database.Models.Chat {
            Id = Guid.NewGuid(),
            ChatType = ChatType.StudentChat,
            TeacherId = teacherId,
            StudentId = studentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync(ct);

        return MapChatToDto(chat);
    }

    private ChatDto MapChatToDto(Database.Models.Chat chat) {
        string participantName = string.Empty;
        string? participantPicture = null;

        if (chat.ChatType == ChatType.ParentChat && chat.Parent?.User != null) {
            participantName = chat.Parent.User.GetFullName();
            participantPicture = chat.Parent.User.ProfilePicture;
        } else if (chat.ChatType == ChatType.StudentChat && chat.Student?.User != null) {
            participantName = chat.Student.User.GetFullName();
            participantPicture = chat.Student.User.ProfilePicture;
        }

        var lastMessage = chat.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

        return new ChatDto {
            ChatId = chat.Id,
            ChatType = chat.ChatType,
            TeacherId = chat.TeacherId,
            ParentId = chat.ParentId,
            StudentId = chat.StudentId,
            ParticipantName = participantName,
            ParticipantProfilePicture = participantPicture,
            LastMessage = lastMessage?.Content ?? null,
            LastMessageTime = lastMessage?.CreatedAt,
            UnreadCount = 0, // TODO: Implement read/unread status
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt,
            IsActive = chat.IsActive
        };
    }
}
