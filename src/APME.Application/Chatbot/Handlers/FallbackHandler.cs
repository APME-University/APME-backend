using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using Volo.Abp.DependencyInjection;

namespace APME.Chatbot.Handlers;

public class FallbackHandler : IIntentHandler, ITransientDependency
{
    public IntentType TargetIntent => IntentType.Unknown;

    public Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var reply = classification.RequiresClarification && classification.ClarificationQuestion is not null
            ? classification.ClarificationQuestion
            : "I'm not quite sure what you're looking for. Could you rephrase your question? I can help with:\n" +
              "- Finding products\n- Comparing products\n- Product details\n- Recommendations\n" +
              "- Browsing categories\n- Order status\n- Shipping & returns policies\n- Account help";

        return Task.FromResult(new IntentHandlerResult { Reply = reply });
    }
}
