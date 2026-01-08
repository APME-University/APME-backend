using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.AI;

/// <summary>
/// Service interface for semantic product search using vector embeddings.
/// SRS Reference: AI Chatbot RAG Architecture - RAG Integration
/// </summary>
public interface ISemanticSearchService : IApplicationService
{
    /// <summary>
    /// Searches for products semantically similar to the query.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="tenantId">Optional tenant filter (null = platform-wide).</param>
    /// <param name="shopId">Optional shop filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results ranked by relevance.</returns>
    Task<List<ProductSearchResult>> SearchAsync(
        string query,
        int topK = 10,
        Guid? tenantId = null,
        Guid? shopId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets similar products based on a given product.
    /// </summary>
    /// <param name="productId">The reference product ID.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of similar products.</returns>
    Task<List<ProductSearchResult>> GetSimilarProductsAsync(
        Guid productId,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a semantic product search.
/// </summary>
public class ProductSearchResult
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Relevance score (0-1, higher = more relevant).
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Product name for display.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Shop ID the product belongs to.
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Shop name for display.
    /// </summary>
    public string? ShopName { get; set; }

    /// <summary>
    /// Category name for display.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Whether the product is in stock.
    /// </summary>
    public bool IsInStock { get; set; }

    /// <summary>
    /// Whether the product is on sale.
    /// </summary>
    public bool IsOnSale { get; set; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string? SKU { get; set; }

    /// <summary>
    /// Snippet of the matched text (for highlighting).
    /// </summary>
    public string? MatchedSnippet { get; set; }
}








