using System.Threading;
using System.Threading.Tasks;
using APME.Chatbot.Intents;

namespace APME.Chatbot;

/// <summary>
/// Domain service interface for LLM-based intent classification.
/// Implementation lives in Application layer (LlmIntentClassifier).
/// </summary>
public interface IIntentClassifier
{
    Task<IntentClassificationResult> ClassifyAsync(
        ClassificationContext context,
        CancellationToken ct = default);
}
