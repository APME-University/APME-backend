using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Implementation of chat context builder.
/// Loads session history and builds prompts for LLM consumption.
/// </summary>
public class ChatContextBuilder : IChatContextBuilder, ITransientDependency
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatMemoryManager _memoryManager;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatContextBuilder> _logger;

    private const string SystemPrompt = @"You are a helpful e-commerce assistant for an online shopping platform. 
Your role is to help customers find products, answer questions about products, and provide shopping recommendations.

When answering questions:
1. Use the provided product context to give accurate, specific information
2. If asked about products not in the context, politely say you don't have information about those specific products
3. Be helpful, friendly, and concise
4. If you recommend products, explain why they might be good choices
5. Always provide accurate prices and availability information from the context
6. Do not make up product information that is not provided in the context
7. Reference previous conversation context when relevant

Current product context is provided below. Use this information to answer customer questions.";

    public ChatContextBuilder(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IChatMemoryManager memoryManager,
        IOptions<ChatOptions> options,
        ILogger<ChatContextBuilder> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _memoryManager = memoryManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChatContext> LoadContextAsync(
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

        // Load messages within retention period
        var retentionDate = DateTime.UtcNow.AddDays(-_options.MessageRetentionDays);
        var messages = await _messageRepository.GetMessagesInRangeAsync(
            sessionId,
            retentionDate,
            DateTime.UtcNow,
            cancellationToken);

        var context = new ChatContext
        {
            SessionId = sessionId,
            CustomerId = customerId,
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SequenceNumber = m.SequenceNumber,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreationTime,
                Metadata = m.Metadata
            }).ToList()
        };

        // Apply sliding window
        context.Messages = _memoryManager.ApplySlidingWindow(
            context.Messages,
            _options.MaxContextMessages);

        // Prune to token budget
        var pruneResult = _memoryManager.PruneToTokenBudget(
            context.Messages,
            _options.MaxContextTokens);

        context.Messages = pruneResult.PrunedMessages;
        context.EstimatedTokenCount = pruneResult.FinalTokenCount;
        context.WasPruned = pruneResult.RemovedMessageCount > 0;
        context.PrunedMessageCount = pruneResult.RemovedMessageCount;

        _logger.LogDebug(
            "Loaded context for session {SessionId}: {MessageCount} messages, {TokenCount} tokens",
            sessionId,
            context.Messages.Count,
            context.EstimatedTokenCount);

        return context;
    }

    public string BuildPrompt(
        ChatContext context,
        string currentMessage,
        int maxTokens = 4000)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemPrompt);
        sb.AppendLine();

        // Add product context if available
        if (context.ContextProducts.Count > 0)
        {
            sb.AppendLine("=== PRODUCT CONTEXT ===");
            sb.AppendLine();

            foreach (var product in context.ContextProducts)
            {
                sb.AppendLine($"Product: {product.ProductName}");
                sb.AppendLine($"- Price: ${product.Price:F2}");
                sb.AppendLine($"- In Stock: {(product.IsInStock ? "Yes" : "No")}");
                sb.AppendLine($"- On Sale: {(product.IsOnSale ? "Yes" : "No")}");

                if (!string.IsNullOrWhiteSpace(product.CategoryName))
                {
                    sb.AppendLine($"- Category: {product.CategoryName}");
                }

                if (!string.IsNullOrWhiteSpace(product.ShopName))
                {
                    sb.AppendLine($"- Shop: {product.ShopName}");
                }

                if (!string.IsNullOrWhiteSpace(product.MatchedSnippet))
                {
                    sb.AppendLine($"- Details: {product.MatchedSnippet}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("=== END PRODUCT CONTEXT ===");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("No specific products found matching the query. Provide general assistance.");
            sb.AppendLine();
        }

        // Add conversation history
        if (context.Messages.Count > 0)
        {
            sb.AppendLine("=== CONVERSATION HISTORY ===");
            sb.AppendLine();

            foreach (var message in context.Messages)
            {
                var roleLabel = message.Role == ChatMessageRole.User ? "Customer" : "Assistant";
                sb.AppendLine($"{roleLabel}: {message.Content}");
                sb.AppendLine();
            }

            sb.AppendLine("=== END CONVERSATION HISTORY ===");
            sb.AppendLine();
        }

        // Add current message
        sb.AppendLine($"Customer Question: {currentMessage}");

        return sb.ToString();
    }

    public List<AI.ChatMessage> ToChatMessages(ChatContext context)
    {
        return context.Messages.Select(m => new AI.ChatMessage
        {
            Role = m.Role == ChatMessageRole.User ? "user" : "assistant",
            Content = m.Content,
            Timestamp = m.CreatedAt
        }).ToList();
    }
}




