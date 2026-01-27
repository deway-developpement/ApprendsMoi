using Microsoft.AspNetCore.SignalR;
using backend.Helpers;

namespace backend.Domains.Chat;

public class ChatHub : Hub {
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger) {
        _logger = logger;
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
    /// </summary>
    public async Task JoinChat(string chatId) {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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
    /// </summary>
    public async Task SendMessageToChat(string chatId, object message) {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"User {userId} sent message in chat {chatId}");
        
        // Send to all users in this chat group
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
