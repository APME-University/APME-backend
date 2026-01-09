using System;
using System.Collections.Generic;

namespace APME.AI;

/// <summary>
/// DTO for chat request from API.
/// </summary>
public class ChatRequestDto
{
    /// <summary>
    /// The user's message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant filter.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Optional shop filter.
    /// </summary>
    public Guid? ShopId { get; set; }

    /// <summary>
    /// Number of context products to use.
    /// </summary>
    public int ContextProductCount { get; set; } = 5;

    /// <summary>
    /// Session ID for conversation tracking.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Previous messages in the conversation.
    /// </summary>
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}

/// <summary>
/// DTO for chat message in conversation history.
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// Role: user, assistant, or system.
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// Message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO for chat response.
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// The AI-generated response.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to generate response in milliseconds.
    /// </summary>
    public long GenerationTimeMs { get; set; }

    /// <summary>
    /// Session ID for conversation tracking.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Products used as context.
    /// </summary>
    public List<ProductSearchResultDto> ContextProducts { get; set; } = new();
}

/// <summary>
/// DTO for semantic search request.
/// </summary>
public class SemanticSearchRequestDto
{
    /// <summary>
    /// The search query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int TopK { get; set; } = 10;

    /// <summary>
    /// Optional tenant filter.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Optional shop filter.
    /// </summary>
    public Guid? ShopId { get; set; }
}

/// <summary>
/// DTO for similar products request.
/// </summary>
public class SimilarProductsRequestDto
{
    /// <summary>
    /// The product to find similar products for.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int TopK { get; set; } = 5;
}

/// <summary>
/// DTO for product search result.
/// </summary>
public class ProductSearchResultDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Relevance score (0-1).
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Shop ID.
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Shop name.
    /// </summary>
    public string? ShopName { get; set; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Whether in stock.
    /// </summary>
    public bool IsInStock { get; set; }

    /// <summary>
    /// Whether on sale.
    /// </summary>
    public bool IsOnSale { get; set; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string? SKU { get; set; }

    /// <summary>
    /// Matched text snippet.
    /// </summary>
    public string? MatchedSnippet { get; set; }
}

/// <summary>
/// DTO for AI health check result.
/// </summary>
public class AIHealthCheckDto
{
    /// <summary>
    /// Whether Ollama server is connected.
    /// </summary>
    public bool OllamaConnected { get; set; }

    /// <summary>
    /// Whether the embedding model is available.
    /// </summary>
    public bool EmbeddingModelAvailable { get; set; }

    /// <summary>
    /// Whether the generation model is available.
    /// </summary>
    public bool GenerationModelAvailable { get; set; }

    /// <summary>
    /// Current embedding model name.
    /// </summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// Current generation model name.
    /// </summary>
    public string GenerationModel { get; set; } = string.Empty;

    /// <summary>
    /// Total embedding count.
    /// </summary>
    public long TotalEmbeddings { get; set; }

    /// <summary>
    /// Active embedding count.
    /// </summary>
    public long ActiveEmbeddings { get; set; }

    /// <summary>
    /// Error message if any.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for bulk reindex result.
/// </summary>
public class BulkReindexResultDto
{
    public bool Success { get; set; }
    public int TotalProducts { get; set; }
    public int JobsEnqueued { get; set; }
    public long DurationMs { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// DTO for embedding statistics.
/// </summary>
public class EmbeddingStatisticsDto
{
    public long TotalEmbeddings { get; set; }
    public long ActiveEmbeddings { get; set; }
    public long InactiveEmbeddings { get; set; }
    public long UniqueProducts { get; set; }
    public long OutdatedEmbeddings { get; set; }
    public long ProductsNeedingEmbedding { get; set; }
    public int CurrentModelVersion { get; set; }
    public string CurrentModelName { get; set; } = string.Empty;
    public Dictionary<string, long> EmbeddingsByModel { get; set; } = new();
    public Dictionary<int, long> EmbeddingsByVersion { get; set; } = new();
}









