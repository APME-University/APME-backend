using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Intents;

namespace APME.Chatbot.Handlers;

/// <summary>
/// Strategy interface — one implementation per IntentType.
/// Registered in DI; resolved at runtime by IntentHandlerFactory.
/// </summary>
public interface IIntentHandler
{
    IntentType TargetIntent { get; }

    Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default);
}
