using backend.Domains.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Domains.Chat.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessageController(
    MessageService messageService,
    ChatAttachmentService attachmentService) : ControllerBase {
    
    private readonly MessageService _messageService = messageService;
    private readonly ChatAttachmentService _attachmentService = attachmentService;

    /// <summary>
    /// Get messages for a chat with pagination
    /// </summary>
    [HttpGet("{chatId}")]
    public async Task<ActionResult<PaginatedMessagesDto>> GetMessages(
        Guid chatId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _messageService.UserIsParticipantInChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var messages = await _messageService.GetMessagesByChatAsync(chatId, pageNumber, pageSize, ct);
        return Ok(messages);
    }

    /// <summary>
    /// Send a message to a chat
    /// </summary>
    [HttpPost("{chatId}")]
    public async Task<ActionResult<MessageDto>> SendMessage(
        Guid chatId,
        [FromForm] CreateMessageDto dto,
        [FromForm] List<IFormFile>? attachments = null,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _messageService.UserIsParticipantInChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        try {
            // Create message
            var message = await _messageService.CreateMessageAsync(chatId, userGuid, dto, ct);

            // Handle attachments if provided
            if (attachments != null && attachments.Count > 0) {
                var attachmentDtos = new List<ChatAttachmentDto>();
                foreach (var attachment in attachments) {
                    try {
                        var attachmentDto = await _attachmentService.UploadAttachmentToMessageAsync(
                            message.MessageId,
                            chatId,
                            attachment,
                            userGuid,
                            ct);
                        attachmentDtos.Add(attachmentDto);
                    } catch (InvalidOperationException ex) {
                        // Log warning but continue with other files
                        Console.WriteLine($"Failed to upload attachment: {ex.Message}");
                    }
                }
                message.Attachments = attachmentDtos;
            }

            return CreatedAtAction(nameof(GetMessages), new { chatId }, message);
        } catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Search messages in a chat
    /// </summary>
    [HttpGet("{chatId}/search")]
    public async Task<ActionResult<List<MessageDto>>> SearchMessages(
        Guid chatId,
        [FromQuery] string searchTerm,
        CancellationToken ct = default) {
        
        if (string.IsNullOrWhiteSpace(searchTerm)) {
            return BadRequest("Search term is required");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _messageService.UserIsParticipantInChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var messages = await _messageService.SearchMessagesAsync(chatId, searchTerm, ct);
        return Ok(messages);
    }

    /// <summary>
    /// Get recent messages (for real-time sync)
    /// </summary>
    [HttpGet("{chatId}/recent")]
    public async Task<ActionResult<List<MessageDto>>> GetRecentMessages(
        Guid chatId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _messageService.UserIsParticipantInChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var messages = await _messageService.GetRecentMessagesAsync(chatId, limit, ct);
        return Ok(messages);
    }
}
