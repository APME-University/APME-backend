using System.Threading.Tasks;

namespace APME.Chat;

/// <summary>
/// Transport layer abstraction for SignalR operations.
/// Enables testability and decouples the orchestration layer from SignalR specifics.
/// </summary>
public interface ITransportLayer
{
    /// <summary>
    /// Sends a complete message to a specific connection.
    /// </summary>
    Task SendMessageAsync(string connectionId, string message);

    /// <summary>
    /// Sends a streaming token to a specific connection.
    /// </summary>
    Task SendTokenAsync(string connectionId, string token);

    /// <summary>
    /// Sends an error notification to a specific connection.
    /// </summary>
    Task SendErrorAsync(string connectionId, string errorCode, string errorMessage);

    /// <summary>
    /// Notifies a connection that a session was created.
    /// </summary>
    Task NotifySessionCreatedAsync(string connectionId, string sessionId);

    /// <summary>
    /// Notifies a connection that a session was joined.
    /// </summary>
    Task NotifySessionJoinedAsync(string connectionId, string sessionId);

    /// <summary>
    /// Notifies a connection that message processing is complete.
    /// </summary>
    Task NotifyMessageCompleteAsync(string connectionId, string sessionId, ChatMessageResponseDto response);

    /// <summary>
    /// Adds a connection to a session group.
    /// </summary>
    Task AddToSessionGroupAsync(string connectionId, string sessionId);

    /// <summary>
    /// Removes a connection from a session group.
    /// </summary>
    Task RemoveFromSessionGroupAsync(string connectionId, string sessionId);
}
