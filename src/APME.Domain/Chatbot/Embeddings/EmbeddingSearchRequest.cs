using System;
using Pgvector;

namespace APME.Chatbot.Embeddings;

/// <summary>
/// Carries the query vector AND hard-filter constraints extracted by the intent classifier.
/// </summary>
public class EmbeddingSearchRequest
{
    /// <summary>
    /// Query vector — populated by HybridSearchService before calling the vector store.
    /// </summary>
    public Vector? QueryVector { get; set; }

    /// <summary>
    /// Broad recall count; filter narrows this down.
    /// </summary>
    public int TopK { get; init; } = 25;

    /// <summary>
    /// Model name to filter vectors by (prevents mixing incompatible vector spaces).
    /// </summary>
    public string ModelName { get; init; } = string.Empty;

    // Hard filters — null means "no constraint"

    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public decimal? MaxPrice { get; init; }
    public decimal? MinPrice { get; init; }
    public bool InStockOnly { get; init; } = true;
}
