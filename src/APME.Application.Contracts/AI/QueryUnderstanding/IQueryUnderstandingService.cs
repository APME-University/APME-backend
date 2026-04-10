using System.Threading;
using System.Threading.Tasks;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Main interface for query understanding service.
/// Orchestrates intent classification, entity recognition, query rewriting, and expansion.
/// </summary>
public interface IQueryUnderstandingService
{
    /// <summary>
    /// Performs complete query analysis including intent, entities, rewriting, and expansion.
    /// </summary>
    /// <param name="query">The user's query text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete query analysis result.</returns>
    Task<QueryAnalysisResult> AnalyzeQueryAsync(
        string query,
        CancellationToken cancellationToken = default);
}
