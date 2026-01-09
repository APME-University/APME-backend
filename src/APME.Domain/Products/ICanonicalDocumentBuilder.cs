using System;
using System.Threading.Tasks;

namespace APME.Products;

/// <summary>
/// Service for building canonical product documents for AI embeddings.
/// SRS Reference: AI Chatbot RAG Architecture - Canonical Document Generation
/// </summary>
public interface ICanonicalDocumentBuilder
{
    /// <summary>
    /// Builds a canonical document for a product by aggregating product data,
    /// category information, shop context, and normalized dynamic attributes.
    /// </summary>
    /// <param name="product">The product entity.</param>
    /// <returns>The built canonical document.</returns>
    Task<CanonicalProductDocument> BuildAsync(Product product);

    /// <summary>
    /// Gets the current schema version for canonical documents.
    /// </summary>
    int CurrentSchemaVersion { get; }
}









