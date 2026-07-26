using System;
using System.Collections.Generic;

namespace APME.Chatbot.Handlers;

public class IntentHandlerResult
{
    public string Reply { get; init; } = string.Empty;

    public IReadOnlyList<Guid> ReferencedProductIds { get; init; } = [];

    public IReadOnlyList<Guid> ReferencedCategoryIds { get; init; } = [];

    public string? SuggestedIntent { get; init; }
}
