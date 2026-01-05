using System;
using Volo.Abp.EventBus;

namespace APME.Events;

/// <summary>
/// Base Event Transfer Object for product changes.
/// Used to trigger embedding generation via the transactional outbox pattern.
/// SRS Reference: AI Chatbot RAG Architecture - Transactional Outbox
/// </summary>
[EventName("APME.Product.Changed")]
public class ProductChangedEto
{
    /// <summary>
    /// The product ID that changed.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// The shop ID the product belongs to.
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenancy support.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Product name for logging and debugging.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public ProductChangeType ChangeType { get; set; }

    /// <summary>
    /// Schema version of the canonical document when this event was created.
    /// Used for idempotency checking in the embedding pipeline.
    /// </summary>
    public int CanonicalDocumentVersion { get; set; }

    /// <summary>
    /// Timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the product is active and published (eligible for embedding).
    /// </summary>
    public bool IsEligibleForEmbedding { get; set; }
}

/// <summary>
/// Event published when a new product is created.
/// </summary>
[EventName("APME.Product.Created")]
public class ProductCreatedEto : ProductChangedEto
{
    public ProductCreatedEto()
    {
        ChangeType = ProductChangeType.Created;
    }
}

/// <summary>
/// Event published when a product is updated.
/// </summary>
[EventName("APME.Product.Updated")]
public class ProductUpdatedEto : ProductChangedEto
{
    public ProductUpdatedEto()
    {
        ChangeType = ProductChangeType.Updated;
    }

    /// <summary>
    /// Fields that were modified (for selective re-embedding).
    /// </summary>
    public string[]? ModifiedFields { get; set; }
}

/// <summary>
/// Event published when a product is deleted.
/// </summary>
[EventName("APME.Product.Deleted")]
public class ProductDeletedEto : ProductChangedEto
{
    public ProductDeletedEto()
    {
        ChangeType = ProductChangeType.Deleted;
    }
}

/// <summary>
/// Types of product changes that can trigger embedding updates.
/// </summary>
public enum ProductChangeType
{
    /// <summary>
    /// Product was created - needs new embedding.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Product was updated - may need embedding refresh.
    /// </summary>
    Updated = 1,

    /// <summary>
    /// Product was deleted - remove embeddings.
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// Product was published - create or enable embedding.
    /// </summary>
    Published = 3,

    /// <summary>
    /// Product was unpublished - may remove from search.
    /// </summary>
    Unpublished = 4,

    /// <summary>
    /// Bulk reindex requested - regenerate all embeddings.
    /// </summary>
    BulkReindex = 5
}



