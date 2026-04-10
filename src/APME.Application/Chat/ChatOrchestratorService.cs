using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using APME.AI.QueryUnderstanding;
using APME.BlobStorage;
using APME.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;

namespace APME.Chat;

/// <summary>
/// Orchestrator service for chat operations.
/// Coordinates RAG retrieval, context building, LLM generation, and persistence.
/// Uses layer isolation with IChatContextService, IQueryUnderstandingService, and IFallbackStrategyProvider.
/// </summary>
public class ChatOrchestratorService : IChatOrchestratorService, ITransientDependency
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatContextService _contextService;
    private readonly IChatContextBuilder _contextBuilder;
    private readonly AI.QueryUnderstanding.IQueryUnderstandingService _queryUnderstandingService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IFallbackStrategyProvider _fallbackStrategyProvider;
    private readonly IAIChatService _aiChatService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IImageUrlProvider _imageUrlProvider;
    private readonly IDataFilter _dataFilter;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatOrchestratorService> _logger;

    public ChatOrchestratorService(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IChatContextService contextService,
        IChatContextBuilder contextBuilder,
        AI.QueryUnderstanding.IQueryUnderstandingService queryUnderstandingService,
        ISemanticSearchService semanticSearchService,
        IFallbackStrategyProvider fallbackStrategyProvider,
        IAIChatService aiChatService,
        IRepository<Product, Guid> productRepository,
        IImageUrlProvider imageUrlProvider,
        IDataFilter dataFilter,
        IGuidGenerator guidGenerator,
        IOptions<ChatOptions> options,
        ILogger<ChatOrchestratorService> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _contextService = contextService;
        _contextBuilder = contextBuilder;
        _queryUnderstandingService = queryUnderstandingService;
        _semanticSearchService = semanticSearchService;
        _fallbackStrategyProvider = fallbackStrategyProvider;
        _aiChatService = aiChatService;
        _productRepository = productRepository;
        _imageUrlProvider = imageUrlProvider;
        _dataFilter = dataFilter;
        _guidGenerator = guidGenerator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProcessMessageResult> ProcessMessageAsync(
        Guid sessionId,
        Guid customerId,
        string message,
        Func<string, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        _logger.LogInformation(
            "Processing message for session {SessionId}, customer {CustomerId}",
            sessionId,
            customerId);

        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} not found or does not belong to customer {customerId}");
        }

        session.UpdateActivity();
        await _sessionRepository.UpdateAsync(session, cancellationToken: cancellationToken);

        QueryAnalysis? queryAnalysis = null;
        if (_options.EnableQueryUnderstanding)
        {
            var analysisResult = await _queryUnderstandingService.AnalyzeQueryAsync(message, cancellationToken);
            queryAnalysis = QueryAnalysisAdapter.ToLegacyQueryAnalysis(analysisResult);
            _logger.LogDebug(
                "Query analysis: Intent={Intent}, Confidence={Confidence}, ShouldUseRag={ShouldUseRag}, ProcessingTime={ProcessingTime}ms",
                analysisResult.Intent.PrimaryIntent,
                analysisResult.Confidence,
                analysisResult.ShouldUseRag,
                analysisResult.ProcessingTime.TotalMilliseconds);
        }

        var context = await _contextService.LoadSessionContextAsync(
            sessionId,
            customerId,
            cancellationToken);

        context = await _contextService.OptimizeContextAsync(
            context,
            message,
            cancellationToken);

        string assistantContent;
        List<ProductSearchResult> contextProducts;

        if (_options.EnableFallbackStrategies && queryAnalysis != null)
        {
            var fallbackResponse = await _fallbackStrategyProvider.GenerateWithFallbackAsync(
                message,
                context,
                queryAnalysis,
                onToken,
                cancellationToken);

            assistantContent = fallbackResponse.Response;
            // Enrich products from fallback response with image URLs and slugs, limit to top 2
            var limitedFallbackProducts = fallbackResponse.ContextProducts.Take(2).ToList();
            contextProducts = await EnrichProductDataAsync(limitedFallbackProducts, cancellationToken);

            _logger.LogDebug(
                "Response generated via strategy: {Strategy}, Confidence: {Confidence}",
                fallbackResponse.StrategyUsed,
                fallbackResponse.Confidence);
        }
        else
        {
            var ragProducts = await _semanticSearchService.SearchAsync(
                message,
                Math.Min(_options.ContextProductCount, 2), // Limit to top 2 products
                tenantId: null,
                shopId: null,
                cancellationToken);

            contextProducts = await EnrichProductDataAsync(ragProducts, cancellationToken);
            context.ContextProducts = contextProducts;

            var chatMessages = _contextBuilder.ToChatMessages(context);
            var chatRequest = new ChatRequest
            {
                Message = message,
                ConversationHistory = chatMessages,
                SessionId = sessionId.ToString(),
                ContextProductCount = _options.ContextProductCount
            };

            var assistantResponse = new System.Text.StringBuilder();
            // Pass the pre-searched and enriched products to ChatStreamAsync
            // This ensures the AI response uses the same products we'll return to the frontend
            await foreach (var token in _aiChatService.ChatStreamAsync(chatRequest, contextProducts, cancellationToken))
            {
                assistantResponse.Append(token);
                await onToken(token);
            }

            assistantContent = assistantResponse.ToString();
        }

        var nextSequence = await _messageRepository.GetNextSequenceNumberAsync(
            sessionId,
            cancellationToken);

        var userMessage = new ChatMessage(
            _guidGenerator.Create(),
            sessionId,
            nextSequence,
            ChatMessageRole.User,
            message);

        if (queryAnalysis != null)
        {
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                Intent = queryAnalysis.Intent.ToString(),
                Confidence = queryAnalysis.Confidence,
                ProcessedQuery = queryAnalysis.ProcessedQuery,
                EntityCount = queryAnalysis.Entities.Count
            });
            userMessage.SetMetadata(metadata);
        }

        await _messageRepository.InsertAsync(userMessage, cancellationToken: cancellationToken);

        var assistantSequence = await _messageRepository.GetNextSequenceNumberAsync(
            sessionId,
            cancellationToken);

        var assistantMessage = new ChatMessage(
            _guidGenerator.Create(),
            sessionId,
            assistantSequence,
            ChatMessageRole.Assistant,
            assistantContent);

        if (contextProducts.Count > 0)
        {
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                ContextProductIds = contextProducts.Select(p => p.ProductId).ToList(),
                ContextProductCount = contextProducts.Count
            });
            assistantMessage.SetMetadata(metadata);
        }

        await _messageRepository.InsertAsync(assistantMessage, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Message processed successfully. Session {SessionId}, Response length: {Length}",
            sessionId,
            assistantContent.Length);

        return new ProcessMessageResult
        {
            Response = assistantContent,
            ContextProducts = contextProducts
        };
    }

    /// <summary>
    /// Enriches search results with authoritative product data from the relational database.
    /// </summary>
    private async Task<List<ProductSearchResult>> EnrichProductDataAsync(
        List<ProductSearchResult> searchResults,
        CancellationToken cancellationToken)
    {
        if (searchResults.Count == 0)
        {
            return searchResults;
        }

        var productIds = searchResults.Select(r => r.ProductId).ToList();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var products = await _productRepository.GetListAsync(
                p => productIds.Contains(p.Id),
                cancellationToken: cancellationToken);

            var productLookup = products.ToDictionary(p => p.Id);

            foreach (var result in searchResults)
            {
                if (productLookup.TryGetValue(result.ProductId, out var product))
                {
                    result.ProductName = product.Name;
                    result.Price = product.Price;
                    result.IsInStock = product.IsInStock();
                    result.IsOnSale = product.IsOnSale();
                    result.SKU = product.SKU;
                    result.Slug = product.Slug;
                    
                    // Get image URL - will be null if product has no image
                    result.ImageUrl = _imageUrlProvider.GetFullImageUrl(product.PrimaryImageUrl);
                }
                else
                {
                    // Product not found in repository - log warning but continue
                    _logger.LogWarning(
                        "Product {ProductId} from semantic search not found in repository",
                        result.ProductId);
                }
            }
        }

        return searchResults;
    }

    public async Task<ChatSessionDto> GetOrCreateSessionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetOrCreateActiveSessionAsync(
            customerId,
            cancellationToken);

        return new ChatSessionDto
        {
            Id = session.Id,
            CustomerId = session.CustomerId,
            Status = session.Status,
            LastActivityAt = session.LastActivityAt,
            Title = session.Title
        };
    }

    public async Task<ChatSessionDto?> GetSessionAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            return null;
        }

        return new ChatSessionDto
        {
            Id = session.Id,
            CustomerId = session.CustomerId,
            Status = session.Status,
            LastActivityAt = session.LastActivityAt,
            Title = session.Title
        };
    }

    public async Task<List<ChatMessageResponseDto>> GetRecentMessagesAsync(
        Guid sessionId,
        Guid customerId,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} not found or does not belong to customer {customerId}");
        }

        var messages = await _messageRepository.GetRecentMessagesAsync(
            sessionId,
            count,
            cancellationToken);

        return messages.Select(m => new ChatMessageResponseDto
        {
            MessageId = m.Id,
            SessionId = sessionId.ToString(),
            Role = m.Role,
            Content = m.Content,
            CreatedAt = m.CreationTime
        }).ToList();
    }

    public async Task ArchiveSessionAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} not found or does not belong to customer {customerId}");
        }

        session.Archive();
        await _sessionRepository.UpdateAsync(session, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Session {SessionId} archived for customer {CustomerId}",
            sessionId,
            customerId);
    }

    public async Task<List<ChatSessionDto>> GetAllSessionsAsync(
        Guid customerId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync(
            customerId,
            skipCount,
            maxResultCount,
            cancellationToken);

        return sessions.Select(s => new ChatSessionDto
        {
            Id = s.Id,
            CustomerId = s.CustomerId,
            Status = s.Status,
            LastActivityAt = s.LastActivityAt,
            Title = s.Title
        }).ToList();
    }
}
