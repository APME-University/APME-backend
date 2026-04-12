using System;
using Pgvector;
using Volo.Abp.Domain.Entities;

namespace APME.AI;

public class ImageEmbedding : Entity<Guid>
{
    public Guid? ProductImageId { get; private set; }

    public string ImageUrl { get; private set; }

    public Vector Embedding { get; private set; } = null!;

    public string ModelName { get; private set; }

    public int Dimensions { get; private set; }

    public DateTime GeneratedAt { get; private set; }

    protected ImageEmbedding()
    {
    }

    public ImageEmbedding(
        Guid id,
        Guid? productImageId,
        string imageUrl,
        Vector embedding,
        string modelName,
        int dimensions) : base(id)
    {
        ProductImageId = productImageId;
        ImageUrl = imageUrl;
        Embedding = embedding;
        ModelName = modelName;
        Dimensions = dimensions;
        GeneratedAt = DateTime.UtcNow;
    }

    public void UpdateEmbedding(Vector embedding, string modelName, int dimensions)
    {
        Embedding = embedding;
        ModelName = modelName;
        Dimensions = dimensions;
        GeneratedAt = DateTime.UtcNow;
    }
}
