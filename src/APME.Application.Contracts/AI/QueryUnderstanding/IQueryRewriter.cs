using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Interface for query rewriting service.
/// Improves ambiguous or unclear queries for better search results.
/// </summary>
public interface IQueryRewriter
{
    /// <summary>
    /// Rewrites a query to improve clarity and search effectiveness.
    /// </summary>
    /// <param name="query">The original query.</param>
    /// <param name="intentResult">Intent classification result.</param>
    /// <param name="entities">Extracted entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query rewrite result.</returns>
    Task<QueryRewriteResult> RewriteQueryAsync(
        string query,
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities,
        CancellationToken cancellationToken = default);
}
