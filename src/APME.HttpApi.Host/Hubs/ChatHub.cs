using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.Security.Claims;

namespace APME.HttpApi.Host.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Handles bidirectional communication between client and server.
/// Uses Customer-specific authentication to bypass ABP IdentityUser resolution.
/// </summary>
[Authorize(Policy = "CustomerSignalR")]
public class ChatHub : Hub
{
    private readonly IChatOrchestratorService _orchestrator;
    private readonly ChatRateLimiter _rateLimiter;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatOrchestratorService orchestrator,
        ChatRateLimiter rateLimiter,
        ILogger<ChatHub> logger)
    {
        _orchestrator = orchestrator;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            // Log authentication details for debugging
            _logger.LogInformation(
                "SignalR connection attempt: {ConnectionId}, Authenticated: {IsAuthenticated}, User: {UserId}",
                Context.ConnectionId,
                Context.User?.Identity?.IsAuthenticated ?? false,
                Context.User?.Identity?.Name ?? "Unknown");

            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning(
                    "Unauthenticated connection attempt: {ConnectionId}, Claims: {Claims}",
                    Context.ConnectionId,
                    string.Join(", ", Context.User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
                Context.Abort();
                return;
            }

            var customerId = GetCustomerId();
            _logger.LogInformation(
                "Client connected: {ConnectionId}, Customer: {CustomerId}",
                Context.ConnectionId,
                customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate connection: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var customerId = GetCustomerId();
            _logger.LogInformation(
                "Client disconnected: {ConnectionId}, Customer: {CustomerId}",
                Context.ConnectionId,
                customerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get customer ID on disconnect: {ConnectionId}", Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "Disconnect error for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Joins or creates a chat session.
    /// </summary>
    public async Task JoinSession(string sessionId)
    {
        var customerId = GetCustomerId();

        try
        {
            Guid sessionGuid;
            if (!Guid.TryParse(sessionId, out sessionGuid))
            {
                // Create new session
                var session = await _orchestrator.GetOrCreateSessionAsync(customerId);
                await Clients.Caller.SendAsync("SessionCreated", session.Id.ToString());
                _logger.LogInformation("Created new session {SessionId} for customer {CustomerId}", session.Id, customerId);
                return;
            }

            // Validate session belongs to customer
            var sessionDto = await _orchestrator.GetSessionAsync(sessionGuid, customerId);
            if (sessionDto == null)
            {
                await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
                {
                    ErrorCode = "SESSION_NOT_FOUND",
                    Message = "Session not found or access denied"
                });
                return;
            }

            // Join group for this session
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("SessionJoined", sessionId);
            _logger.LogInformation("Customer {CustomerId} joined session {SessionId}", customerId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "JOIN_ERROR",
                Message = "Failed to join session"
            });
        }
    }

    /// <summary>
    /// Sends a message and streams the response.
    /// </summary>
    public async Task SendMessage(string sessionId, string message)
    {
        var customerId = GetCustomerId();

        if (string.IsNullOrWhiteSpace(message))
        {
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "INVALID_MESSAGE",
                Message = "Message cannot be empty"
            });
            return;
        }

        // Rate limiting check
        if (!_rateLimiter.CanSendMessage(customerId))
        {
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "RATE_LIMIT_EXCEEDED",
                Message = "Too many messages. Please wait a moment before sending another message."
            });
            return;
        }

        // Input validation: max length
        if (message.Length > 2000)
        {
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "MESSAGE_TOO_LONG",
                Message = "Message is too long. Maximum length is 2000 characters."
            });
            return;
        }

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "INVALID_SESSION",
                Message = "Invalid session ID"
            });
            return;
        }

        try
        {
            // Stream tokens as they arrive
            var fullResponse = await _orchestrator.ProcessMessageAsync(
                sessionGuid,
                customerId,
                message,
                async token =>
                {
                    await Clients.Caller.SendAsync("ReceiveToken", token);
                },
                CancellationToken.None);

            // Send completion notification
            await Clients.Caller.SendAsync("MessageComplete", new ChatMessageResponseDto
            {
                SessionId = sessionId,
                Role = ChatMessageRole.Assistant,
                Content = fullResponse,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Message processed for session {SessionId}, response length: {Length}",
                sessionId,
                fullResponse.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "PROCESSING_ERROR",
                Message = "Failed to process message. Please try again."
            });
        }
    }

    /// <summary>
    /// Ends a session gracefully.
    /// </summary>
    public async Task EndSession(string sessionId)
    {
        var customerId = GetCustomerId();

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("SessionEnded", sessionId);
            _logger.LogInformation("Customer {CustomerId} ended session {SessionId}", customerId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Archives a session and removes the connection from the session group.
    /// </summary>
    public async Task ArchiveSession(string sessionId)
    {
        var customerId = GetCustomerId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "INVALID_SESSION",
                Message = "Invalid session ID"
            });
            return;
        }

        try
        {
            // Archive the session
            await _orchestrator.ArchiveSessionAsync(sessionGuid, customerId);

            // Remove connection from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

            // Notify client that session was archived
            await Clients.Caller.SendAsync("SessionArchived", sessionId);

            _logger.LogInformation(
                "Customer {CustomerId} archived session {SessionId}",
                customerId,
                sessionId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to archive session {SessionId}: {Message}", sessionId, ex.Message);
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "SESSION_NOT_FOUND",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("ReceiveError", new ChatErrorDto
            {
                ErrorCode = "ARCHIVE_ERROR",
                Message = "Failed to archive session. Please try again."
            });
        }
    }

    /// <summary>
    /// Gets the customer ID from the current user claims.
    /// Works with Customer authentication scheme that bypasses ABP IdentityUser resolution.
    /// </summary>
    private Guid GetCustomerId()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Get customer ID from claims (CustomerSignalR authentication handler sets these)
        var userIdClaim = Context.User?.FindFirst(AbpClaimTypes.UserId)
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirst("sub");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var customerId))
        {
            return customerId;
        }

        throw new UnauthorizedAccessException("Customer ID not found in claims");
    }
}

