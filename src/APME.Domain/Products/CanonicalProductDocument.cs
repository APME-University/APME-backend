using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace APME.Products;

/// <summary>
/// Canonical, flattened representation of a product optimized for AI embeddings.
/// This value object provides a stable, versioned format for generating vector embeddings.
/// SRS Reference: AI Chatbot RAG Architecture - Canonical Product Document
/// </summary>
public class CanonicalProductDocument
{
    /// <summary>
    /// Schema version for forward compatibility during embedding model changes.
    /// Increment when document structure changes significantly.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// The product identifier for linking back to source data.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// The shop identifier for tenant-aware operations.
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenancy support.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Product name - primary search term.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description - rich content for semantic understanding.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category name for contextual categorization.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Product SKU for exact matching.
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Current price for context in searches.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Shop name for multi-shop context.
    /// </summary>
    public string? ShopName { get; set; }

    /// <summary>
    /// Normalized dynamic attributes with semantic labels.
    /// Key: Attribute name, Value: Canonical attribute representation.
    /// </summary>
    public Dictionary<string, CanonicalAttribute> Attributes { get; set; } = new();

    /// <summary>
    /// Timestamp when this document was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the product is currently in stock.
    /// </summary>
    public bool IsInStock { get; set; }

    /// <summary>
    /// Whether the product is on sale.
    /// </summary>
    public bool IsOnSale { get; set; }

    /// <summary>
    /// Converts the canonical document to a text representation optimized for embedding.
    /// Uses semantic labels and structured formatting for better AI understanding.
    /// </summary>
    /// <returns>Flattened text suitable for embedding generation.</returns>
    public string ToEmbeddingText()
    {
        var sb = new StringBuilder();

        // Product name is most important - appears first
        sb.AppendLine($"Product: {Name}");

        // Shop context
        if (!string.IsNullOrWhiteSpace(ShopName))
        {
            sb.AppendLine($"Shop: {ShopName}");
        }

        // Category for contextual understanding
        if (!string.IsNullOrWhiteSpace(CategoryName))
        {
            sb.AppendLine($"Category: {CategoryName}");
        }

        // Description - key for semantic search
        if (!string.IsNullOrWhiteSpace(Description))
        {
            sb.AppendLine($"Description: {Description}");
        }

        // Price information
        sb.AppendLine($"Price: ${Price:F2}");

        // Availability indicators
        if (IsOnSale)
        {
            sb.AppendLine("This product is currently on sale.");
        }
        if (!IsInStock)
        {
            sb.AppendLine("This product is currently out of stock.");
        }

        // Dynamic attributes with semantic labels
        if (Attributes.Count > 0)
        {
            sb.AppendLine("Specifications:");
            
            // Sort by priority (higher first) for embedding relevance
            var sortedAttributes = Attributes
                .OrderByDescending(a => a.Value.Priority)
                .ThenBy(a => a.Key);

            foreach (var attr in sortedAttributes)
            {
                var label = attr.Value.SemanticLabel ?? attr.Key;
                sb.AppendLine($"- {label}: {attr.Value.Value}");
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Serializes the document to JSON for storage.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Deserializes a canonical document from JSON.
    /// </summary>
    public static CanonicalProductDocument? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CanonicalProductDocument>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents a normalized attribute value with semantic context.
/// </summary>
public class CanonicalAttribute
{
    /// <summary>
    /// The attribute value as string.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The data type of the original attribute.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductAttributeDataType DataType { get; set; }

    /// <summary>
    /// Human-readable semantic label for AI context.
    /// </summary>
    public string? SemanticLabel { get; set; }

    /// <summary>
    /// Priority for embedding inclusion. Higher = more important.
    /// </summary>
    public int Priority { get; set; }
}








