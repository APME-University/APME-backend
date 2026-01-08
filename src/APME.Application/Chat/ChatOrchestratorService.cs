using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace APME.Chat;

/// <summary>
/// Orchestrator service for chat operations.
/// Coordinates RAG retrieval, context building, LLM generation, and persistence.
/// </summary>
public class ChatOrchestratorService : IChatOrchestratorService, ITransientDependency
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatContextBuilder _contextBuilder;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IAIChatService _aiChatService;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatOrchestratorService> _logger;

    public ChatOrchestratorService(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IChatContextBuilder contextBuilder,
        ISemanticSearchService semanticSearchService,
        IAIChatService aiChatService,
        IGuidGenerator guidGenerator,
        IOptions<ChatOptions> options,
        ILogger<ChatOrchestratorService> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _contextBuilder = contextBuilder;
        _semanticSearchService = semanticSearchService;
        _aiChatService = aiChatService;
        _guidGenerator = guidGenerator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(
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

        // Validate session belongs to customer
        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} not found or does not belong to customer {customerId}");
        }

        // Update session activity
        session.UpdateActivity();
        await _sessionRepository.UpdateAsync(session, cancellationToken: cancellationToken);

        // Load chat context
        var context = await _contextBuilder.LoadContextAsync(
            sessionId,
            customerId,
            cancellationToken);

        // Perform RAG retrieval
        var contextProducts = await _semanticSearchService.SearchAsync(
            message,
            _options.ContextProductCount,
            tenantId: null, // Platform-wide search
            shopId: null,
            cancellationToken);

        context.ContextProducts = contextProducts;

        // Build chat request
        var chatMessages = _contextBuilder.ToChatMessages(context);
        var chatRequest = new ChatRequest
        {
            Message = message,
            ConversationHistory = chatMessages,
            SessionId = sessionId.ToString(),
            ContextProductCount = _options.ContextProductCount
        };

        // Save user message
        var nextSequence = await _messageRepository.GetNextSequenceNumberAsync(
            sessionId,
            cancellationToken);

        var userMessage = new ChatMessage(
            _guidGenerator.Create(),
            sessionId,
            nextSequence,
            ChatMessageRole.User,
            message);

        await _messageRepository.InsertAsync(userMessage, cancellationToken: cancellationToken);

        // Stream response from AI
        var assistantResponse = new System.Text.StringBuilder();
        await foreach (var token in _aiChatService.ChatStreamAsync(chatRequest, cancellationToken))
        {
            assistantResponse.Append(token);
            await onToken(token);
        }

        var assistantContent = assistantResponse.ToString();

        // Save assistant message
        var assistantSequence = await _messageRepository.GetNextSequenceNumberAsync(
            sessionId,
            cancellationToken);

        var assistantMessage = new ChatMessage(
            _guidGenerator.Create(),
            sessionId,
            assistantSequence,
            ChatMessageRole.Assistant,
            assistantContent);

        // Store metadata about context products
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

        return assistantContent;
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
        // Validate session belongs to customer
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
        // Validate session belongs to customer
        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} not found or does not belong to customer {customerId}");
        }

        // Archive the session
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

