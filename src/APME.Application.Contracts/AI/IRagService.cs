using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APME.AI;

/// <summary>
/// Enhanced RAG service interface with hybrid search and query analysis.
/// Provides retrieval-augmented generation capabilities.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Retrieves relevant documents based on the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Maximum number of results.</param>
    /// <param name="relevanceThreshold">Minimum relevance score (0-1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of retrieved documents with relevance scores.</returns>
    Task<List<RetrievedDocument>> RetrieveRelevantDocumentsAsync(
        string query,
        int topK = 10,
        float relevanceThreshold = 0.45f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves relevant documents with tenant/shop filtering.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Maximum number of results.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="shopId">Optional shop filter.</param>
    /// <param name="relevanceThreshold">Minimum relevance score.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of retrieved documents.</returns>
    Task<List<RetrievedDocument>> RetrieveRelevantDocumentsAsync(
        string query,
        int topK,
        Guid? tenantId,
        Guid? shopId,
        float relevanceThreshold = 0.45f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs hybrid search combining semantic and keyword search.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="analysis">Query analysis results.</param>
    /// <param name="topK">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of retrieved documents.</returns>
    Task<List<RetrievedDocument>> HybridSearchAsync(
        string query,
        QueryAnalysis analysis,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets similar documents based on a reference document.
    /// </summary>
    /// <param name="documentId">The reference document ID.</param>
    /// <param name="topK">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of similar documents.</returns>
    Task<List<RetrievedDocument>> GetSimilarDocumentsAsync(
        Guid documentId,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A document retrieved from the RAG system.
/// </summary>
public class RetrievedDocument
{
    /// <summary>
    /// The document/product ID.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// The product ID (alias for DocumentId).
    /// </summary>
    public Guid ProductId => DocumentId;

    /// <summary>
    /// Relevance score (0-1, higher = more relevant).
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// The matched text content/snippet.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Document/product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Shop ID the document belongs to.
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Shop name for display.
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
    /// Image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Product slug for URL routing.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Converts to ProductSearchResult for backwards compatibility.
    /// </summary>
    public ProductSearchResult ToProductSearchResult()
    {
        return new ProductSearchResult
        {
            ProductId = DocumentId,
            RelevanceScore = RelevanceScore,
            ProductName = Name,
            ShopId = ShopId,
            ShopName = ShopName,
            CategoryName = CategoryName,
            Price = Price,
            IsInStock = IsInStock,
            IsOnSale = IsOnSale,
            SKU = SKU,
            MatchedSnippet = Content,
            ImageUrl = ImageUrl,
            Slug = Slug
        };
    }
}
