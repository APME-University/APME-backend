using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APME.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.AI;

/// <summary>
/// Implementation of semantic product search using vector embeddings.
/// SRS Reference: AI Chatbot RAG Architecture - RAG Integration
/// </summary>
public class SemanticSearchService : ISemanticSearchService, ITransientDependency
{
    private readonly IProductEmbeddingRepository _embeddingRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IOllamaEmbeddingService _embeddingService;
    private readonly AIOptions _options;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(
        IProductEmbeddingRepository embeddingRepository,
        IRepository<Product, Guid> productRepository,
        IOllamaEmbeddingService embeddingService,
        IOptions<AIOptions> options,
        IDataFilter dataFilter,
        ILogger<SemanticSearchService> logger)
    {
        _embeddingRepository = embeddingRepository;
        _productRepository = productRepository;
        _embeddingService = embeddingService;
        _options = options.Value;
        _dataFilter = dataFilter;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductSearchResult>> SearchAsync(
        string query,
        int topK = 10,
        Guid? tenantId = null,
        Guid? shopId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<ProductSearchResult>();
        }

        _logger.LogInformation(
            "Semantic search: '{Query}' (topK={TopK}, tenant={TenantId}, shop={ShopId})",
            query, topK, tenantId, shopId);

        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

            // Search for similar products
            var searchResults = await _embeddingRepository.SearchSimilarAsync(
                queryEmbedding,
                topK * 2, // Get extra results to handle deduplication across chunks
                tenantId,
                shopId,
                activeOnly: true,
                cancellationToken);

            // Deduplicate by product ID (multiple chunks may match)
            var deduplicatedResults = searchResults
                .GroupBy(r => r.Embedding.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    // Take highest score among chunks
                    BestMatch = g.OrderByDescending(r => r.SimilarityScore).First()
                })
                .OrderByDescending(r => r.BestMatch.SimilarityScore)
                .Take(topK)
                .ToList();

            // Build result DTOs with payload metadata
            var results = new List<ProductSearchResult>();
            
            foreach (var match in deduplicatedResults)
            {
                var result = BuildSearchResult(match.BestMatch);
                results.Add(result);
            }

            _logger.LogInformation(
                "Semantic search completed: {ResultCount} results for '{Query}'",
                results.Count, query);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic search failed for query: {Query}", query);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ProductSearchResult>> GetSimilarProductsAsync(
        Guid productId,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding similar products for {ProductId}", productId);

        try
        {
            // Get embeddings for the reference product
            var productEmbeddings = await _embeddingRepository.GetByProductIdAsync(
                productId, cancellationToken);

            if (productEmbeddings.Count == 0)
            {
                _logger.LogWarning("No embeddings found for product {ProductId}", productId);
                return new List<ProductSearchResult>();
            }

            // Use the first chunk's embedding (primary embedding)
            var referenceEmbedding = productEmbeddings.First().Embedding;

            // Search for similar products (exclude the reference product)
            var searchResults = await _embeddingRepository.SearchSimilarAsync(
                referenceEmbedding,
                (topK + 1) * 2, // Extra for deduplication and excluding self
                tenantId: null,
                shopId: null,
                activeOnly: true,
                cancellationToken);

            // Deduplicate and exclude reference product
            var deduplicatedResults = searchResults
                .Where(r => r.Embedding.ProductId != productId)
                .GroupBy(r => r.Embedding.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    BestMatch = g.OrderByDescending(r => r.SimilarityScore).First()
                })
                .OrderByDescending(r => r.BestMatch.SimilarityScore)
                .Take(topK)
                .ToList();

            var results = deduplicatedResults
                .Select(m => BuildSearchResult(m.BestMatch))
                .ToList();

            _logger.LogDebug(
                "Found {Count} similar products for {ProductId}",
                results.Count, productId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar products for {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Builds a ProductSearchResult from embedding search result.
    /// </summary>
    private ProductSearchResult BuildSearchResult(ProductEmbeddingSearchResult match)
    {
        var result = new ProductSearchResult
        {
            ProductId = match.Embedding.ProductId,
            ShopId = match.Embedding.ShopId,
            RelevanceScore = match.SimilarityScore,
            MatchedSnippet = TruncateText(match.Embedding.ChunkText, 200)
        };

        // Parse payload if available
        if (!string.IsNullOrWhiteSpace(match.Embedding.PayloadJson))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<ProductPayload>(
                    match.Embedding.PayloadJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload != null)
                {
                    result.ProductName = payload.Name ?? string.Empty;
                    result.ShopName = payload.ShopName;
                    result.CategoryName = payload.CategoryName;
                    result.Price = payload.Price;
                    result.IsInStock = payload.IsInStock;
                    result.IsOnSale = payload.IsOnSale;
                    result.SKU = payload.SKU;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to parse payload for embedding {EmbeddingId}",
                    match.Embedding.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// Truncates text to a maximum length with ellipsis.
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text ?? string.Empty;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Internal class for deserializing embedding payload.
    /// </summary>
    private class ProductPayload
    {
        public Guid ProductId { get; set; }
        public string? Name { get; set; }
        public Guid ShopId { get; set; }
        public string? ShopName { get; set; }
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public bool IsInStock { get; set; }
        public bool IsOnSale { get; set; }
        public string? SKU { get; set; }
    }
}



