using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateProductAttributeDto
{
    [Required]
    public Guid ShopId { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string DisplayName { get; set; }

    [Required]
    public ProductAttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this attribute should be included in AI embeddings.
    /// Defaults to true for semantic search relevance.
    /// </summary>
    public bool IncludeInEmbedding { get; set; } = true;

    /// <summary>
    /// Priority for embedding inclusion. Higher = more important.
    /// </summary>
    public int EmbeddingPriority { get; set; } = 0;

    /// <summary>
    /// Human-readable semantic label for AI embedding context.
    /// Example: "Color" -> "product color"
    /// </summary>
    [StringLength(256)]
    public string? SemanticLabel { get; set; }
}

