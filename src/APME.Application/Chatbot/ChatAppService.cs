using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Dtos;
using APME.Chatbot.Intents;
using APME.Products;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Settings;
using Volo.Abp.Uow;

namespace APME.Chatbot;

public class ChatAppService : ApplicationService, IChatAppService
{
    private readonly IIntentClassifier _classifier;
    private readonly IntentHandlerFactory _handlerFactory;
    private readonly ConversationContextManager _contextManager;
    private readonly IChatSessionRepository _sessionRepo;
    private readonly IChatMessageRepository _messageRepo;
    private readonly IRepository<IntentClassificationLog, Guid> _logRepo;
    private readonly IRepository<Product, Guid> _productRepo;
    private readonly ISettingProvider _settings;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILogger<ChatAppService> _logger;

    public ChatAppService(
        IIntentClassifier classifier,
        IntentHandlerFactory handlerFactory,
        ConversationContextManager contextManager,
        IChatSessionRepository sessionRepo,
        IChatMessageRepository messageRepo,
        IRepository<IntentClassificationLog, Guid> logRepo,
        IRepository<Product, Guid> productRepo,
        ISettingProvider settings,
        IGuidGenerator guidGenerator,
        ILogger<ChatAppService> logger)
    {
        _classifier = classifier;
        _handlerFactory = handlerFactory;
        _contextManager = contextManager;
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _logRepo = logRepo;
        _productRepo = productRepo;
        _settings = settings;
        _guidGenerator = guidGenerator;
        _logger = logger;
    }

    [UnitOfWork]
    public virtual async Task<ChatResponseDto> SendMessageAsync(SendMessageInput input)
    {
        var sessionId = Guid.Parse(input.SessionId);
        var sw = Stopwatch.StartNew();

        // 1. Get or create session
        var session = await _sessionRepo.GetAsync(sessionId);

        // 2. Persist user message
        var seqNum = await _messageRepo.GetNextSequenceNumberAsync(sessionId);
        var userMsg = new ChatMessage(
            _guidGenerator.Create(), sessionId, seqNum, ChatMessageRole.User, input.Message);
        await _messageRepo.InsertAsync(userMsg);

        // 3. Build classification context
        var context = await _contextManager.BuildAsync(sessionId, input.Message);

        // 4. Classify intent via LLM
        var classification = await _classifier.ClassifyAsync(context);

        _logger.LogInformation(
            "Intent classified: Session={SessionId}, Intent={Intent}, Confidence={Confidence:F2}, Entities={EntityCount}, Clarification={RequiresClarification}",
            sessionId, classification.Intent, classification.Confidence,
            classification.Entities.Count, classification.RequiresClarification);

        // 5. Log classification
        sw.Stop();
        var log = new IntentClassificationLog(
            _guidGenerator.Create(),
            userMsg.Id,
            classification.Intent,
            classification.Confidence,
            (int)sw.ElapsedMilliseconds);
        await _logRepo.InsertAsync(log);

        // 6. Update user message with classification
        userMsg.SetMetadata(System.Text.Json.JsonSerializer.Serialize(new
        {
            intent = classification.Intent.ToString(),
            confidence = classification.Confidence,
            entities = classification.Entities,
            processingTimeMs = sw.ElapsedMilliseconds
        }));

        // 7. Route to handler
        var handler = _handlerFactory.GetHandler(classification.Intent);
        var result = await handler.HandleAsync(classification, context);

        // 8. Persist assistant message
        var assistantSeq = seqNum + 1;
        var assistantMsg = new ChatMessage(
            _guidGenerator.Create(), sessionId, assistantSeq, ChatMessageRole.Assistant, result.Reply);
        assistantMsg.SetMetadata(System.Text.Json.JsonSerializer.Serialize(new
        {
            intent = classification.Intent.ToString(),
            referencedProductIds = result.ReferencedProductIds,
            referencedCategoryIds = result.ReferencedCategoryIds
        }));
        await _messageRepo.InsertAsync(assistantMsg);

        // 9. Persist conversation context
        await _contextManager.PersistAsync(
            sessionId, classification, result.ReferencedProductIds);

        // 10. Build response DTO
        var products = new List<ProductSummaryDto>();
        if (result.ReferencedProductIds.Count > 0)
        {
            var prods = (await _productRepo.GetListAsync(
                p => result.ReferencedProductIds.Contains(p.Id)))
                .Take(10).ToList();
            products = prods.Select(MapToProductSummary).ToList();
        }

        return new ChatResponseDto
        {
            Reply = result.Reply,
            Intent = classification.Intent.ToString(),
            Confidence = classification.Confidence,
            Products = products,
            ClarificationQuestion = classification.ClarificationQuestion,
            SuggestedIntent = result.SuggestedIntent
        };
    }

    private ProductSummaryDto MapToProductSummary(Product p)
    {
        return new ProductSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ShortDescription = p.ShortDescription,
            Price = p.Price,
            SalePrice = p.SalePrice,
            Currency = p.Currency,
            BrandName = p.Brand?.Name,
            InStock = p.StockQuantity > 0
        };
    }
}
