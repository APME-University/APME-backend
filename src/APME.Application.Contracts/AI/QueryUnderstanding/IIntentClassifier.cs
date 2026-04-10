using System.Threading;
using System.Threading.Tasks;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Interface for intent classification service.
/// Uses hybrid approach: fast rule-based classification with LLM fallback.
/// </summary>
public interface IIntentClassifier
{
    /// <summary>
    /// Classifies the intent of a user query.
    /// </summary>
    /// <param name="query">The user's query text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Intent classification result with confidence scores.</returns>
    Task<IntentClassificationResult> ClassifyIntentAsync(
        string query,
        CancellationToken cancellationToken = default);
}
