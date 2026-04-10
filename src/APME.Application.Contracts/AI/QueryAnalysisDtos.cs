using System;
using System.Collections.Generic;

namespace APME.AI;

/// <summary>
/// User intent classifications for query understanding.
/// </summary>
public enum Intent
{
    /// <summary>
    /// Unknown or unclassified intent.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Greeting or conversation starter.
    /// </summary>
    Greeting = 1,

    /// <summary>
    /// Farewell or conversation ender.
    /// </summary>
    Farewell = 2,

    /// <summary>
    /// Product discovery/browsing.
    /// </summary>
    ProductDiscovery = 10,

    /// <summary>
    /// Specific product details request.
    /// </summary>
    ProductDetail = 11,

    /// <summary>
    /// Product comparison request.
    /// </summary>
    Comparison = 12,

    /// <summary>
    /// Price inquiry.
    /// </summary>
    PriceInquiry = 13,

    /// <summary>
    /// Availability/stock inquiry.
    /// </summary>
    AvailabilityInquiry = 14,

    /// <summary>
    /// Order management (status, history).
    /// </summary>
    OrderManagement = 20,

    /// <summary>
    /// Cart/checkout assistance.
    /// </summary>
    CartAssistance = 21,

    /// <summary>
    /// Support/help request.
    /// </summary>
    Support = 30,

    /// <summary>
    /// Human agent request.
    /// </summary>
    HumanHandoff = 31,

    /// <summary>
    /// General question or chitchat.
    /// </summary>
    General = 100
}

/// <summary>
/// Result of query analysis.
/// </summary>
public class QueryAnalysis
{
    /// <summary>
    /// The original query text.
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Processed/normalized query for search.
    /// </summary>
    public string ProcessedQuery { get; set; } = string.Empty;

    /// <summary>
    /// Detected user intent.
    /// </summary>
    public Intent Intent { get; set; } = Intent.Unknown;

    /// <summary>
    /// Confidence score for the detected intent (0-1).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Extracted entities from the query.
    /// </summary>
    public List<Entity> Entities { get; set; } = new();

    /// <summary>
    /// Detected keywords for search.
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Whether the analysis fell back to rule-based.
    /// </summary>
    public bool FallbackToRuleBased { get; set; }

    /// <summary>
    /// Suggested search filters based on analysis.
    /// </summary>
    public Dictionary<string, object> SuggestedFilters { get; set; } = new();

    /// <summary>
    /// Whether RAG retrieval should be used for this query.
    /// </summary>
    public bool ShouldUseRag => Intent switch
    {
        Intent.ProductDiscovery => true,
        Intent.ProductDetail => true,
        Intent.Comparison => true,
        Intent.PriceInquiry => true,
        Intent.AvailabilityInquiry => true,
        _ => false
    };

    /// <summary>
    /// Whether this query requires human handoff.
    /// </summary>
    public bool RequiresHumanHandoff => Intent == Intent.HumanHandoff;
}

/// <summary>
/// An entity extracted from a query.
/// </summary>
public class Entity
{
    /// <summary>
    /// The entity type (Price, Category, Brand, Color, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The extracted value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for the extraction (0-1).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Start position in the original query.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// End position in the original query.
    /// </summary>
    public int EndIndex { get; set; }

    /// <summary>
    /// Normalized value for search/filtering.
    /// </summary>
    public string? NormalizedValue { get; set; }
}

/// <summary>
/// Result of entity extraction.
/// </summary>
public class EntityAnalysis
{
    /// <summary>
    /// Extracted entities.
    /// </summary>
    public List<Entity> Entities { get; set; } = new();

    /// <summary>
    /// Overall confidence in the extraction.
    /// </summary>
    public float Confidence { get; set; }
}

/// <summary>
/// Legacy interface for query understanding service.
/// Use APME.AI.QueryUnderstanding.IQueryUnderstandingService instead.
/// </summary>
[Obsolete("Use APME.AI.QueryUnderstanding.IQueryUnderstandingService instead")]
public interface IQueryUnderstandingService
{
    /// <summary>
    /// Analyzes a user query to determine intent and extract entities.
    /// </summary>
    /// <param name="query">The user's query text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results.</returns>
    System.Threading.Tasks.Task<QueryAnalysis> AnalyzeAsync(
        string query,
        System.Threading.CancellationToken cancellationToken = default);
}
