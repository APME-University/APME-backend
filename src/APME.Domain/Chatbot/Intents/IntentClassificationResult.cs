using System;
using System.Collections.Generic;
using System.Linq;
using APME.Chat;

namespace APME.Chatbot.Intents;

/// <summary>
/// Immutable value object — the LLM's parsed answer.
/// </summary>
public class IntentClassificationResult
{
    public IntentType Intent { get; init; }

    public float Confidence { get; init; }

    public IReadOnlyList<ExtractedEntity> Entities { get; init; } = [];

    public bool RequiresClarification { get; init; }

    public string? ClarificationQuestion { get; init; }

    /// <summary>
    /// Only populated when Intent == ProductSearch.
    /// The LLM rewrites the user's natural-language description into a
    /// keyword-optimised query for vector search (Phase 3).
    /// e.g. "something to keep coffee warm hiking" →
    ///      "insulated travel mug thermos outdoor hiking heat retention"
    /// </summary>
    public string? RewrittenQuery { get; init; }

    /// <summary>
    /// Convenience helper — checks if confidence meets threshold.
    /// </summary>
    public bool IsHighConfidence(float threshold = 0.65f) => Confidence >= threshold;

    /// <summary>
    /// Gets the first entity value matching the given type, or null.
    /// </summary>
    public string? GetEntity(string type) =>
        Entities.FirstOrDefault(e => e.Type == type)?.Value;
}
