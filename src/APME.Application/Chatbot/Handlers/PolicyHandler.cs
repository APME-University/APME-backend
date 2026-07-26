using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using Volo.Abp.DependencyInjection;

namespace APME.Chatbot.Handlers;

public class PolicyHandler : IIntentHandler, ITransientDependency
{
    public IntentType TargetIntent => IntentType.Policy;

    public Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var topic = classification.GetEntity("policy_topic")?.ToLowerInvariant();

        var reply = topic switch
        {
            "shipping" or "delivery" =>
                "We offer standard shipping (5-7 business days) and express shipping (1-2 business days). Free standard shipping on orders over 200 SAR!",
            "returns" or "return" or "refund" =>
                "You can return items within 14 days of delivery. Items must be unused and in original packaging. Refunds are processed within 5-7 business days.",
            "warranty" =>
                "All electronics come with a minimum 1-year manufacturer warranty. Extended warranty options are available at checkout.",
            "payment" =>
                "We accept credit/debit cards (Visa, Mastercard, Mada), Apple Pay, and cash on delivery (COD) for orders under 5,000 SAR.",
            _ => "I can help with information about shipping, returns, warranty, or payment policies. Which would you like to know about?"
        };

        return Task.FromResult(new IntentHandlerResult { Reply = reply });
    }
}
