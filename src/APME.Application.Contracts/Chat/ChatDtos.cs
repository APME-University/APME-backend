using System;
using System.Collections.Generic;
using APME.AI;

namespace APME.Chat;

/// <summary>
/// DTOs for SignalR chat communication.
/// </summary>

/// <summary>
/// Request to send a message via SignalR.
/// </summary>
public class SendMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response sent back via SignalR for a complete message.
/// </summary>
public class ChatMessageResponseDto
{
    public Guid MessageId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ProductSearchResultDto>? ContextProducts { get; set; }
}

/// <summary>
/// DTO for product search results in chat context.
/// </summary>
public class ProductSearchResultDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsInStock { get; set; }
    public bool IsOnSale { get; set; }
    public string? CategoryName { get; set; }
    public string? ShopName { get; set; }
    public string? MatchedSnippet { get; set; }
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Session information DTO.
/// </summary>
public class ChatSessionDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public ChatSessionStatus Status { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string? Title { get; set; }
}

/// <summary>
/// Error response DTO.
/// </summary>
public class ChatErrorDto
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}




