using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Database.Models;
using backend.Helpers;

namespace backend.Domains.Chat.Controllers;

[ApiController]
[Route("api/chats")]
[Authorize]
[RequireRole(ProfileType.Parent, ProfileType.Student, ProfileType.Admin)] // Teachers need separate verification
public class ChatController(ChatService chatService) : ControllerBase {
    private readonly ChatService _chatService = chatService;

    /// <summary>
    /// Get all chats for the current user
    /// </summary>
    [HttpGet]
    [RequireRole(ProfileType.Parent, ProfileType.Student, ProfileType.Admin, ProfileType.Teacher)]
    public async Task<ActionResult<List<ChatDto>>> GetChats(CancellationToken ct = default) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        var profileStr = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(profileStr)) {
            return Unauthorized();
        }

        List<ChatDto> chats = [];

        if (profileStr == ProfileType.Teacher.ToString()) {
            chats = await _chatService.GetChatsByTeacherAsync(userGuid, ct);
        } else if (profileStr == ProfileType.Parent.ToString()) {
            chats = await _chatService.GetChatsByParentAsync(userGuid, ct);
        } else if (profileStr == ProfileType.Student.ToString()) {
            chats = await _chatService.GetChatsByStudentAsync(userGuid, ct);
        } else {
            return Forbid();
        }

        return Ok(chats);
    }

    /// <summary>
    /// Get a specific chat with all messages
    /// </summary>
    [HttpGet("{chatId}")]
    public async Task<ActionResult<ChatDetailDto>> GetChat(Guid chatId, CancellationToken ct = default) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var chat = await _chatService.GetChatByIdAsync(chatId, ct);
        if (chat == null) {
            return NotFound();
        }

        return Ok(chat);
    }

    /// <summary>
    /// Create a new chat
    /// - Teachers can create chats with parents/students
    /// - Parents can initiate chats with any teacher
    /// - Students can initiate chats with their teachers
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatDto>> CreateChat(
        [FromBody] CreateChatDto dto,
        CancellationToken ct = default) {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        var profileStr = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(profileStr)) {
            return Unauthorized();
        }

        // Parse the profile type
        if (!Enum.TryParse<ProfileType>(profileStr, out var userProfile)) {
            return Unauthorized();
        }

        // Only teachers, parents, and students can create chats
        // (not admins - though they could be allowed in future)
        if (userProfile != ProfileType.Teacher && 
            userProfile != ProfileType.Parent && 
            userProfile != ProfileType.Student) {
            return Forbid("Only teachers, parents, and students can create chats");
        }

        try {
            var chat = await _chatService.CreateChatAsync(dto, userGuid, userProfile, ct);
            return CreatedAtAction(nameof(GetChat), new { chatId = chat.ChatId }, chat);
        } catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Archive a chat
    /// </summary>
    [HttpPost("{chatId}/archive")]
    public async Task<IActionResult> ArchiveChat(Guid chatId, CancellationToken ct = default) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var result = await _chatService.ArchiveChatAsync(chatId, ct);
        if (!result) {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Reactivate an archived chat
    /// </summary>
    [HttpPost("{chatId}/reactivate")]
    public async Task<IActionResult> ReactivateChat(Guid chatId, CancellationToken ct = default) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var result = await _chatService.ReactivateChatAsync(chatId, ct);
        if (!result) {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Mark all messages in a chat as read for the current user
    /// </summary>
    [HttpPost("{chatId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid chatId, CancellationToken ct = default) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            return Unauthorized();
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatId, userGuid, ct);
        if (!hasAccess) {
            return Forbid();
        }

        var updated = await _chatService.MarkChatAsReadAsync(chatId, userGuid, ct);
        if (!updated) return NotFound();

        return NoContent();
    }
}
