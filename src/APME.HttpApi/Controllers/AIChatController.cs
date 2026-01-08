using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace APME.Controllers;

/// <summary>
/// API controller for AI-powered chat and semantic search.
/// Provides endpoints for the customer chatbot using RAG architecture.
/// SRS Reference: AI Chatbot RAG Architecture - API Endpoints
/// </summary>
[Route("api/ai")]
public class AIChatController : AbpController
{
    private readonly IAIChatService _chatService;
    private readonly ISemanticSearchService _searchService;
    private readonly IBulkReindexService _reindexService;
    private readonly AIOptions _options;

    public AIChatController(
        IAIChatService chatService,
        ISemanticSearchService searchService,
        IBulkReindexService reindexService,
        IOptions<AIOptions> options)
    {
        _chatService = chatService;
        _searchService = searchService;
        _reindexService = reindexService;
        _options = options.Value;
    }

    /// <summary>
    /// Chat with the AI assistant using RAG for product context.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI-generated response with product context.</returns>
    [HttpPost("chat")]
    [AllowAnonymous] // Public endpoint for customer chatbot
    public async Task<ChatResponseDto> ChatAsync(
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new UserFriendlyException("Message cannot be empty");
        }

        var chatRequest = new ChatRequest
        {
            Message = request.Message,
            TenantId = request.TenantId,
            ShopId = request.ShopId,
            ContextProductCount = request.ContextProductCount,
            SessionId = request.SessionId,
            ConversationHistory = request.ConversationHistory?.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };

        var response = await _chatService.ChatAsync(chatRequest, cancellationToken);

        return new ChatResponseDto
        {
            Response = response.Response,
            GenerationTimeMs = response.GenerationTimeMs,
            SessionId = response.SessionId,
            ContextProducts = response.ContextProducts.Select(MapToDto).ToList()
        };
    }

    /// <summary>
    /// Semantic search for products.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of relevant products.</returns>
    [HttpPost("search")]
    [AllowAnonymous] // Public endpoint
    public async Task<List<ProductSearchResultDto>> SearchAsync(
        [FromBody] SemanticSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new UserFriendlyException("Query cannot be empty");
        }

        var results = await _searchService.SearchAsync(
            request.Query,
            request.TopK,
            request.TenantId,
            request.ShopId,
            cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets products similar to a given product.
    /// </summary>
    /// <param name="request">The request with product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of similar products.</returns>
    [HttpPost("similar")]
    [AllowAnonymous] // Public endpoint
    public async Task<List<ProductSearchResultDto>> GetSimilarProductsAsync(
        [FromBody] SimilarProductsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new UserFriendlyException("ProductId is required");
        }

        var results = await _searchService.GetSimilarProductsAsync(
            request.ProductId,
            request.TopK,
            cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Health check for AI services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health status of AI services.</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<AIHealthCheckDto> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var health = new AIHealthCheckDto
        {
            EmbeddingModel = _options.EmbeddingModel,
            GenerationModel = _options.GenerationModel
        };

        try
        {
            // Check Ollama connection via the reindex service
            health.OllamaConnected = await _reindexService.TestConnectionAsync(cancellationToken);
            health.EmbeddingModelAvailable = health.OllamaConnected;
            health.GenerationModelAvailable = health.OllamaConnected;

            // Get embedding counts
            var (total, active) = await _reindexService.GetEmbeddingCountsAsync(cancellationToken);
            health.TotalEmbeddings = total;
            health.ActiveEmbeddings = active;
        }
        catch (Exception ex)
        {
            health.OllamaConnected = false;
            health.ErrorMessage = ex.Message;
        }

        return health;
    }

    #region Admin Operations

    /// <summary>
    /// Triggers a bulk reindex of all product embeddings.
    /// Admin only.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="shopId">Optional shop filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the bulk reindex operation.</returns>
    [HttpPost("admin/reindex")]
    [Authorize] // Should add admin policy in production
    public async Task<BulkReindexResultDto> TriggerBulkReindexAsync(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? shopId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reindexService.TriggerBulkReindexAsync(
            tenantId, shopId, cancellationToken);

        return new BulkReindexResultDto
        {
            Success = result.Success,
            TotalProducts = result.TotalProducts,
            JobsEnqueued = result.JobsEnqueued,
            DurationMs = result.DurationMs,
            Errors = result.Errors
        };
    }

    /// <summary>
    /// Reindexes only products with outdated embeddings.
    /// Admin only.
    /// </summary>
    /// <param name="batchSize">Number of products to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("admin/reindex-outdated")]
    [Authorize]
    public async Task<BulkReindexResultDto> ReindexOutdatedAsync(
        [FromQuery] int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await _reindexService.ReindexOutdatedEmbeddingsAsync(
            batchSize, cancellationToken);

        return new BulkReindexResultDto
        {
            Success = result.Success,
            TotalProducts = result.TotalProducts,
            JobsEnqueued = result.JobsEnqueued,
            DurationMs = result.DurationMs,
            Errors = result.Errors
        };
    }

    /// <summary>
    /// Gets embedding statistics.
    /// Admin only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("admin/statistics")]
    [Authorize]
    public async Task<EmbeddingStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await _reindexService.GetStatisticsAsync(cancellationToken);

        return new EmbeddingStatisticsDto
        {
            TotalEmbeddings = stats.TotalEmbeddings,
            ActiveEmbeddings = stats.ActiveEmbeddings,
            InactiveEmbeddings = stats.InactiveEmbeddings,
            UniqueProducts = stats.UniqueProducts,
            OutdatedEmbeddings = stats.OutdatedEmbeddings,
            ProductsNeedingEmbedding = stats.ProductsNeedingEmbedding,
            CurrentModelVersion = stats.CurrentModelVersion,
            CurrentModelName = stats.CurrentModelName,
            EmbeddingsByModel = stats.EmbeddingsByModel,
            EmbeddingsByVersion = stats.EmbeddingsByVersion
        };
    }

    #endregion

    /// <summary>
    /// Maps ProductSearchResult to DTO.
    /// </summary>
    private static ProductSearchResultDto MapToDto(ProductSearchResult result)
    {
        return new ProductSearchResultDto
        {
            ProductId = result.ProductId,
            RelevanceScore = result.RelevanceScore,
            ProductName = result.ProductName,
            ShopId = result.ShopId,
            ShopName = result.ShopName,
            CategoryName = result.CategoryName,
            Price = result.Price,
            IsInStock = result.IsInStock,
            IsOnSale = result.IsOnSale,
            SKU = result.SKU,
            MatchedSnippet = result.MatchedSnippet
        };
    }
}
