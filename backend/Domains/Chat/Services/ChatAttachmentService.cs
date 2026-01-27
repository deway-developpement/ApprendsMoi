using backend.Database;
using backend.Database.Models;
using backend.Domains.Chat.Mappers;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Chat;

public class ChatAttachmentService(AppDbContext db, IFileStorageService fileStorageService) {
    private readonly AppDbContext _db = db;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    /// <summary>
    /// Upload and save an attachment to a message
    /// </summary>
    public async Task<ChatAttachmentDto> UploadAttachmentToMessageAsync(
        Guid messageId,
        Guid chatId,
        IFormFile file,
        Guid uploadedBy,
        CancellationToken ct = default) {
        
        // Upload file
        var (fileUrl, fileName, fileSize, fileType) = await _fileStorageService.UploadFileAsync(file, chatId, ct);

        // Create attachment record
        var attachment = new ChatAttachment {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ChatId = chatId,
            FileName = fileName,
            FileUrl = fileUrl,
            FileSize = fileSize,
            FileType = fileType,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatAttachments.Add(attachment);
        await _db.SaveChangesAsync(ct);

        // Load uploader for DTO
        var attachmentWithUploader = await _db.ChatAttachments
            .Where(a => a.Id == attachment.Id)
            .Include(a => a.Uploader)
            .FirstOrDefaultAsync(ct);

        return attachmentWithUploader?.ToDto() ?? new ChatAttachmentDto { AttachmentId = attachment.Id };
    }

    /// <summary>
    /// Upload attachment to chat (shared files, not tied to a message)
    /// </summary>
    public async Task<ChatAttachmentDto> UploadAttachmentToChatAsync(
        Guid chatId,
        IFormFile file,
        Guid uploadedBy,
        CancellationToken ct = default) {
        
        // Upload file
        var (fileUrl, fileName, fileSize, fileType) = await _fileStorageService.UploadFileAsync(file, chatId, ct);

        // Create attachment record without message
        var attachment = new ChatAttachment {
            Id = Guid.NewGuid(),
            MessageId = null,
            ChatId = chatId,
            FileName = fileName,
            FileUrl = fileUrl,
            FileSize = fileSize,
            FileType = fileType,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatAttachments.Add(attachment);
        await _db.SaveChangesAsync(ct);

        // Load uploader for DTO
        var attachmentWithUploader = await _db.ChatAttachments
            .Where(a => a.Id == attachment.Id)
            .Include(a => a.Uploader)
            .FirstOrDefaultAsync(ct);

        return attachmentWithUploader?.ToDto() ?? new ChatAttachmentDto { AttachmentId = attachment.Id };
    }

    /// <summary>
    /// Get all attachments in a chat
    /// </summary>
    public async Task<List<ChatAttachmentDto>> GetAttachmentsByChatAsync(
        Guid chatId,
        CancellationToken ct = default) {
        
        var attachments = await _db.ChatAttachments
            .Where(a => a.ChatId == chatId)
            .Include(a => a.Uploader)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return attachments.ToDtos();
    }

    /// <summary>
    /// Get shared files (attachments not tied to messages) in a chat
    /// </summary>
    public async Task<List<ChatAttachmentDto>> GetSharedFilesByChatAsync(
        Guid chatId,
        CancellationToken ct = default) {
        
        var attachments = await _db.ChatAttachments
            .Where(a => a.ChatId == chatId && a.MessageId == null)
            .Include(a => a.Uploader)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return attachments.ToDtos();
    }

    /// <summary>
    /// Get all shared files uploaded by a teacher
    /// </summary>
    public async Task<List<ChatAttachmentDto>> GetSharedFilesByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default) {
        
        var attachments = await _db.ChatAttachments
            .Where(a => a.Chat != null && a.Chat.TeacherId == teacherId && a.MessageId == null)
            .Include(a => a.Uploader)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return attachments.ToDtos();
    }

    /// <summary>
    /// Delete an attachment
    /// </summary>
    public async Task<bool> DeleteAttachmentAsync(
        Guid attachmentId,
        Guid userId,
        CancellationToken ct = default) {
        
        var attachment = await _db.ChatAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        if (attachment == null) return false;

        // Only uploader can delete their attachment
        if (attachment.UploadedBy != userId) {
            throw new UnauthorizedAccessException("Only the uploader can delete this attachment");
        }

        // Delete from storage
        var deleted = await _fileStorageService.DeleteFileAsync(attachment.FileUrl, ct);

        // Delete from database
        _db.ChatAttachments.Remove(attachment);
        await _db.SaveChangesAsync(ct);

        return deleted;
    }

    /// <summary>
    /// Check if attachment exists and belongs to chat
    /// </summary>
    public async Task<bool> AttachmentBelongsToChatAsync(
        Guid attachmentId,
        Guid chatId,
        CancellationToken ct = default) {
        
        return await _db.ChatAttachments
            .AnyAsync(a => a.Id == attachmentId && a.ChatId == chatId, ct);
    }
}
