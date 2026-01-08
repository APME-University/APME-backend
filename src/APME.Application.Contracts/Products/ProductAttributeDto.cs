using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductAttributeDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }

    public string Name { get; set; }

    public string DisplayName { get; set; }

    public ProductAttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this attribute is included in AI embeddings for semantic search.
    /// </summary>
    public bool IncludeInEmbedding { get; set; }

    /// <summary>
    /// Priority for embedding inclusion. Higher = more important.
    /// </summary>
    public int EmbeddingPriority { get; set; }

    /// <summary>
    /// Human-readable semantic label for embedding context.
    /// </summary>
    public string? SemanticLabel { get; set; }
}

