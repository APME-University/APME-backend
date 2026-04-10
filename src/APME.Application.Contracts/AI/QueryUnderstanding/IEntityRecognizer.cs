using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Interface for entity recognition service.
/// Extracts product-related entities from user queries.
/// </summary>
public interface IEntityRecognizer
{
    /// <summary>
    /// Recognizes entities in a user query.
    /// </summary>
    /// <param name="query">The user's query text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of extracted entities with confidence scores.</returns>
    Task<List<EntityExtractionResult>> RecognizeEntitiesAsync(
        string query,
        CancellationToken cancellationToken = default);
}
