using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using Volo.Abp.DependencyInjection;

namespace APME.Chatbot.Handlers;

public class OffTopicHandler : IIntentHandler, ITransientDependency
{
    public IntentType TargetIntent => IntentType.OffTopic;

    public Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var reply = classification.Intent == IntentType.OffTopic
            ? "I'm here to help you with products, orders, and shopping! What can I assist you with?"
            : "Hello! How can I help you today?";

        return Task.FromResult(new IntentHandlerResult { Reply = reply });
    }
}
