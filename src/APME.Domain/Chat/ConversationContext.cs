using System;
using Volo.Abp.Domain.Entities;

namespace APME.Chat;

public class ConversationContext : Entity<Guid>
{
    public Guid SessionId { get; set; }

    /// <summary>
    /// JSON: {"lastViewedProductIds":[12,45],"compareList":[12,45],
    ///        "activeFilters":{"categoryId":3,"maxPrice":1500},"turnCount":6}
    /// </summary>
    public string ContextJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ChatSession Session { get; set; } = null!;

    protected ConversationContext()
    {
    }

    public ConversationContext(
        Guid id,
        Guid sessionId,
        string contextJson = "{}") : base(id)
    {
        SessionId = sessionId;
        ContextJson = contextJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContext(string contextJson)
    {
        ContextJson = contextJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
