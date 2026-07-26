using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using Volo.Abp.DependencyInjection;

namespace APME.Chatbot.Handlers;

public class AccountManagementHandler : IIntentHandler, ITransientDependency
{
    public IntentType TargetIntent => IntentType.AccountManagement;

    public Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        // TODO: Integrate with identity/account module when available
        var reply = "I can help with account-related questions! For security reasons, please manage your account settings through your profile page. " +
                    "You can update your password, shipping addresses, and payment methods there. Is there anything else I can help with?";

        return Task.FromResult(new IntentHandlerResult { Reply = reply });
    }
}
