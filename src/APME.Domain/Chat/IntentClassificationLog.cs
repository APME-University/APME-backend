using System;
using Volo.Abp.Domain.Entities;

namespace APME.Chat;

public class IntentClassificationLog : Entity<Guid>
{
    public Guid ChatMessageId { get; private set; }

    public IntentType Intent { get; set; }

    public float Confidence { get; set; }

    public string? ModelUsed { get; set; }

    public string? RawResponseJson { get; set; }

    public int ProcessingTimeMs { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual ChatMessage ChatMessage { get; set; } = null!;

    protected IntentClassificationLog()
    {
    }

    public IntentClassificationLog(
        Guid id,
        Guid chatMessageId,
        IntentType intent,
        float confidence,
        int processingTimeMs) : base(id)
    {
        ChatMessageId = chatMessageId;
        Intent = intent;
        Confidence = confidence;
        ProcessingTimeMs = processingTimeMs;
        CreatedAt = DateTime.UtcNow;
    }
}
