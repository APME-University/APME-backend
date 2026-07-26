using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using Volo.Abp.DependencyInjection;

namespace APME.Chatbot.Handlers;

public class OrderStatusHandler : IIntentHandler, ITransientDependency
{
    public IntentType TargetIntent => IntentType.OrderStatus;

    public Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var orderNumber = classification.GetEntity("order_number");

        // TODO: Integrate with order service when available
        var reply = orderNumber is not null
            ? $"I'd be happy to check the status of order **{orderNumber}**. Our order tracking feature is coming soon — please check your account for the latest updates."
            : "I can help you check your order status! Please provide your order number and I'll look it up for you. (Order tracking is coming soon.)";

        return Task.FromResult(new IntentHandlerResult { Reply = reply });
    }
}
