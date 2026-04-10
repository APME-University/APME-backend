using System.Linq;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Adapter to convert between new QueryAnalysisResult and legacy QueryAnalysis types.
/// Provides backward compatibility during migration.
/// </summary>
public static class QueryAnalysisAdapter
{
    /// <summary>
    /// Converts new QueryAnalysisResult to legacy QueryAnalysis.
    /// </summary>
    public static QueryAnalysis ToLegacyQueryAnalysis(QueryAnalysisResult result)
    {
        return new QueryAnalysis
        {
            OriginalQuery = result.OriginalQuery,
            ProcessedQuery = result.ProcessedQuery,
            Intent = MapIntentType(result.Intent.PrimaryIntent),
            Confidence = result.Confidence,
            Entities = result.Entities.Select(e => new Entity
            {
                Type = e.EntityType,
                Value = e.Value,
                NormalizedValue = e.NormalizedValue,
                Confidence = e.Confidence,
                StartIndex = e.StartPosition,
                EndIndex = e.EndPosition
            }).ToList(),
            Keywords = result.Keywords,
            FallbackToRuleBased = result.Intent.UsedRuleBased && !result.Intent.UsedLlmFallback,
            SuggestedFilters = result.SuggestedFilters
        };
    }

    /// <summary>
    /// Maps new IntentType to legacy Intent enum.
    /// </summary>
    public static Intent MapIntentType(IntentType intentType)
    {
        return intentType switch
        {
            IntentType.Informational => Intent.ProductDetail,
            IntentType.Navigational => Intent.ProductDiscovery,
            IntentType.Transactional => Intent.ProductDiscovery,
            IntentType.Support => Intent.Support,
            IntentType.Comparison => Intent.Comparison,
            IntentType.Discovery => Intent.ProductDiscovery,
            IntentType.Account => Intent.OrderManagement,
            IntentType.Greeting => Intent.Greeting,
            IntentType.Farewell => Intent.Farewell,
            IntentType.HumanHandoff => Intent.HumanHandoff,
            _ => Intent.Unknown
        };
    }

    /// <summary>
    /// Maps legacy Intent to new IntentType enum.
    /// </summary>
    public static IntentType MapLegacyIntent(Intent intent)
    {
        return intent switch
        {
            Intent.ProductDiscovery => IntentType.Navigational,
            Intent.ProductDetail => IntentType.Informational,
            Intent.Comparison => IntentType.Comparison,
            Intent.PriceInquiry => IntentType.Transactional,
            Intent.AvailabilityInquiry => IntentType.Informational,
            Intent.OrderManagement => IntentType.Account,
            Intent.CartAssistance => IntentType.Transactional,
            Intent.Support => IntentType.Support,
            Intent.HumanHandoff => IntentType.HumanHandoff,
            Intent.Greeting => IntentType.Greeting,
            Intent.Farewell => IntentType.Farewell,
            Intent.General => IntentType.Informational,
            _ => IntentType.Unknown
        };
    }
}
