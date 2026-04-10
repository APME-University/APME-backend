using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Interface for query expansion service.
/// Generates alternative query variants to improve search coverage.
/// </summary>
public interface IQueryExpander
{
    /// <summary>
    /// Expands a query into multiple search variants.
    /// </summary>
    /// <param name="query">The query to expand.</param>
    /// <param name="entities">Extracted entities for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query expansion result with variants.</returns>
    Task<QueryExpansionResult> ExpandQueryAsync(
        string query,
        List<EntityExtractionResult> entities,
        CancellationToken cancellationToken = default);
}
