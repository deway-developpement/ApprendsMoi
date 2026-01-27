using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using backend.Helpers;
using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Chat;

[Authorize]
public class ChatHub : Hub {
    private readonly ILogger<ChatHub> _logger;
    private readonly AppDbContext _db;
    private readonly ChatService _chatService;

    public ChatHub(ILogger<ChatHub> logger, AppDbContext db, ChatService chatService) {
        _logger = logger;
        _db = db;
        _chatService = chatService;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync() {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"User {userId} connected to chat hub. ConnectionId: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception) {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"User {userId} disconnected from chat hub. ConnectionId: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a chat group (called by client when opening a chat)
    /// Validates that the caller is a participant in the chat before joining
    /// </summary>
    public async Task JoinChat(string chatId) {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            await Clients.Caller.SendAsync("Error", "Unauthorized: Invalid user");
            return;
        }

        if (!Guid.TryParse(chatId, out var chatGuid)) {
            await Clients.Caller.SendAsync("Error", "Invalid chat ID");
            return;
        }

        // Verify user has access to this chat
        var hasAccess = await _chatService.UserHasAccessToChatAsync(chatGuid, userGuid);
        if (!hasAccess) {
            await Clients.Caller.SendAsync("Error", "Unauthorized: You are not a participant in this chat");
            _logger.LogWarning($"User {userId} attempted to join chat {chatId} without access");
            return;
        }

        _logger.LogInformation($"User {userId} joined chat {chatId}");
        
        // Add connection to group named after the chatId
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    /// <summary>
    /// Leave a chat group (called by client when closing a chat)
    /// </summary>
    public async Task LeaveChat(string chatId) {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"User {userId} left chat {chatId}");
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    /// <summary>
    /// Broadcast a new message to all users in the chat
    /// Only allows chat participants to send messages
    /// </summary>
    public async Task SendMessageToChat(string chatId, MessageDto message) {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)) {
            await Clients.Caller.SendAsync("Error", "Unauthorized: Invalid user");
            return;
        }

        // Validate that user is a participant in this chat
        if (!Guid.TryParse(chatId, out var chatGuid)) {
            await Clients.Caller.SendAsync("Error", "Invalid chat ID");
            return;
        }

        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatGuid);
        if (chat == null) {
            await Clients.Caller.SendAsync("Error", "Chat not found");
            return;
        }

        // Check if user is a participant in this chat
        var isParticipant = chat.TeacherId == userGuid || 
                           chat.ParentId == userGuid || 
                           chat.StudentId == userGuid;

        if (!isParticipant) {
            await Clients.Caller.SendAsync("Error", "Unauthorized: You are not a participant in this chat");
            _logger.LogWarning($"User {userId} attempted to send message in chat {chatId} without access");
            return;
        }

        _logger.LogInformation($"User {userId} sent message in chat {chatId}");
        
        // Broadcast to all users in this chat group
        await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", message);
    }

    /// <summary>
    /// Notify chat participants that a user is typing
    /// </summary>
    public async Task UserTyping(string chatId, string userName) {
        await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
            .SendAsync("UserTyping", userName);
    }

    /// <summary>
    /// Notify chat participants that typing has stopped
    /// </summary>
    public async Task UserStoppedTyping(string chatId) {
        await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
            .SendAsync("UserStoppedTyping");
    }
}
