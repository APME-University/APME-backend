using System;
using System.Collections.Generic;

namespace APME.Chatbot.Intents;

/// <summary>
/// Snapshot of everything the classifier needs.
/// Built by ConversationContextManager before the LLM call.
/// </summary>
public class ClassificationContext
{
    public string CurrentMessage { get; init; } = string.Empty;

    public IReadOnlyList<(string Role, string Content)> RecentTurns { get; init; } = [];

    public string? LastIntent { get; init; }

    public IReadOnlyList<Guid> LastViewedProductIds { get; init; } = [];

    public IReadOnlyList<Guid> CompareList { get; init; } = [];

    public string? ActiveCategorySlug { get; init; }
}
