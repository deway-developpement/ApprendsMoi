using backend.Domains.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Database.Models;
using backend.Database;

namespace backend.Domains.Chat.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FileController(
    ChatAttachmentService attachmentService,
    MessageService messageService,
    ChatService chatService,
    AppDbContext db) : ControllerBase {
    
    private readonly ChatAttachmentService _attachmentService = attachmentService;
    private readonly MessageService _messageService = messageService;
    private readonly ChatService _chatService = chatService;
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Upload a file to a chat (shared file, not tied to a message)
    /// </summary>
    [HttpPost("{chatId}")]
    public async Task<ActionResult<ChatAttachmentDto>> UploadFileToChat(
        Guid chatId,
        IFormFile file,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        try {
            var attachment = await _attachmentService.UploadAttachmentToChatAsync(
                chatId,
                file,
                userGuid,
                ct);
            
            return CreatedAtAction(nameof(GetChatFiles), new { chatId }, attachment);
        } catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get all files in a chat
    /// </summary>
    [HttpGet("{chatId}")]
    public async Task<ActionResult<List<ChatAttachmentDto>>> GetChatFiles(
        Guid chatId,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var files = await _attachmentService.GetAttachmentsByChatAsync(chatId, ct);
        return Ok(files);
    }

    /// <summary>
    /// Get shared files in a chat (excluding files in messages)
    /// </summary>
    [HttpGet("{chatId}/shared")]
    public async Task<ActionResult<List<ChatAttachmentDto>>> GetSharedFiles(
        Guid chatId,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var files = await _attachmentService.GetSharedFilesByChatAsync(chatId, ct);
        return Ok(files);
    }

    /// <summary>
    /// Get all shared files uploaded by a teacher
    /// </summary>
    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<List<ChatAttachmentDto>>> GetTeacherFiles(
        Guid teacherId,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Users can only view their own shared files, or teachers can view their own
        var profileStr = User.FindFirst(ClaimTypes.Role)?.Value;
        if (profileStr == ProfileType.Teacher.ToString()) {
            if (userGuid != teacherId) {
                return Forbid();
            }
        } else {
            // For non-teachers, verify they have access to at least one chat with this teacher
            var hasAccess = _db.Chats
                .Any(c => c.TeacherId == teacherId && 
                    (c.ParentId == userGuid || c.StudentId == userGuid));
            if (!hasAccess) {
                return Forbid();
            }
        }

        var files = await _attachmentService.GetSharedFilesByTeacherAsync(teacherId, ct);
        return Ok(files);
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{attachmentId}")]
    public async Task<IActionResult> DeleteFile(
        Guid attachmentId,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        try {
            await _attachmentService.DeleteAttachmentAsync(attachmentId, userGuid, ct);
            return NoContent();
        } catch (UnauthorizedAccessException) {
            return Forbid();
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }
}
