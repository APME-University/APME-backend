using System;
using Volo.Abp.Domain.Entities;

namespace APME.AI;

public class SearchQueryLog : Entity<Guid>
{
    public string Query { get; set; }

    public string? RewrittenQuery { get; set; }

    public string? ClassifiedIntent { get; set; }

    public string? TopResultIdsJson { get; set; }

    public string? SessionId { get; set; }

    public int ProcessingTimeMs { get; set; }

    public DateTime CreatedAt { get; set; }

    protected SearchQueryLog()
    {
    }

    public SearchQueryLog(
        Guid id,
        string query,
        int processingTimeMs) : base(id)
    {
        Query = query;
        ProcessingTimeMs = processingTimeMs;
        CreatedAt = DateTime.UtcNow;
    }
}
