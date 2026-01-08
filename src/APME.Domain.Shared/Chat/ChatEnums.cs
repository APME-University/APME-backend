namespace APME.Chat;

/// <summary>
/// Role of a chat message.
/// </summary>
public enum ChatMessageRole
{
    /// <summary>
    /// Message from the user/customer.
    /// </summary>
    User = 0,

    /// <summary>
    /// Message from the AI assistant.
    /// </summary>
    Assistant = 1
}

/// <summary>
/// Status of a chat session.
/// </summary>
public enum ChatSessionStatus
{
    /// <summary>
    /// Session is active and can receive messages.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Session has been archived by the user.
    /// </summary>
    Archived = 1,

    /// <summary>
    /// Session has expired due to inactivity.
    /// </summary>
    Expired = 2
}


