using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Intents;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace APME.Chatbot;

public class ConversationContextManager : ITransientDependency
{
    private readonly IChatMessageRepository _messageRepo;
    private readonly IRepository<ConversationContext, Guid> _contextRepo;
    private readonly IGuidGenerator _guidGenerator;

    public ConversationContextManager(
        IChatMessageRepository messageRepo,
        IRepository<ConversationContext, Guid> contextRepo,
        IGuidGenerator guidGenerator)
    {
        _messageRepo = messageRepo;
        _contextRepo = contextRepo;
        _guidGenerator = guidGenerator;
    }

    public async Task<ClassificationContext> BuildAsync(
        Guid sessionId,
        string currentMessage,
        CancellationToken ct = default)
    {
        // Load recent turns (last 6 messages)
        var recent = await _messageRepo.GetRecentMessagesAsync(sessionId, 6, ct);

        var turns = recent
            .OrderBy(m => m.SequenceNumber)
            .Select(m => (Role: m.Role.ToString().ToLowerInvariant(), Content: m.Content))
            .ToList();

        // Load persisted context
        var ctx = await _contextRepo.FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

        string? lastIntent = null;
        List<Guid> lastViewedProductIds = [];
        List<Guid> compareList = [];
        string? activeCategorySlug = null;

        if (ctx is not null && !string.IsNullOrWhiteSpace(ctx.ContextJson) && ctx.ContextJson != "{}")
        {
            var doc = JsonDocument.Parse(ctx.ContextJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("lastIntent", out var li) && li.ValueKind == JsonValueKind.String)
                lastIntent = li.GetString();

            if (root.TryGetProperty("lastViewedProductIds", out var lv) && lv.ValueKind == JsonValueKind.Array)
                lastViewedProductIds = lv.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => Guid.Parse(e.GetString()!)).ToList();

            if (root.TryGetProperty("compareList", out var cl) && cl.ValueKind == JsonValueKind.Array)
                compareList = cl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => Guid.Parse(e.GetString()!)).ToList();

            if (root.TryGetProperty("activeCategorySlug", out var ac) && ac.ValueKind == JsonValueKind.String)
                activeCategorySlug = ac.GetString();
        }

        return new ClassificationContext
        {
            CurrentMessage = currentMessage,
            RecentTurns = turns,
            LastIntent = lastIntent,
            LastViewedProductIds = lastViewedProductIds,
            CompareList = compareList,
            ActiveCategorySlug = activeCategorySlug
        };
    }

    public async Task PersistAsync(
        Guid sessionId,
        IntentClassificationResult result,
        IReadOnlyList<Guid> referencedProductIds,
        CancellationToken ct = default)
    {
        var ctx = await _contextRepo.FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

        var payload = new
        {
            lastIntent = result.Intent.ToString(),
            lastViewedProductIds = referencedProductIds,
            compareList = result.Intent == APME.Chat.IntentType.ProductComparison
                ? referencedProductIds
                : (List<Guid>?)null,
            activeCategorySlug = (string?)null,
            turnCount = 0 // updated externally if needed
        };

        var json = JsonSerializer.Serialize(payload);

        if (ctx is null)
        {
            ctx = new ConversationContext(_guidGenerator.Create(), sessionId, json);
            await _contextRepo.InsertAsync(ctx, cancellationToken: ct);
        }
        else
        {
            ctx.UpdateContext(json);
            await _contextRepo.UpdateAsync(ctx, cancellationToken: ct);
        }
    }
}
