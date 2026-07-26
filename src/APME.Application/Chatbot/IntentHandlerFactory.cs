using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace APME.Chatbot;

public class IntentHandlerFactory : ITransientDependency
{
    private readonly IEnumerable<IIntentHandler> _handlers;
    private readonly ILogger<IntentHandlerFactory> _logger;

    public IntentHandlerFactory(
        IEnumerable<IIntentHandler> handlers,
        ILogger<IntentHandlerFactory> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public IIntentHandler GetHandler(IntentType intent)
    {
        var handler = _handlers.FirstOrDefault(h => h.TargetIntent == intent);

        if (handler is null)
        {
            _logger.LogWarning("No handler registered for intent {Intent}, falling back to Unknown", intent);
            handler = _handlers.FirstOrDefault(h => h.TargetIntent == IntentType.Unknown);
        }

        if (handler is null)
        {
            _logger.LogError("No handler registered for intent {Intent} and no FallbackHandler found — using inline fallback", intent);
            handler = new InlineFallbackHandler();
        }

        return handler;
    }

    /// <summary>
    /// Safety-net handler used when no IIntentHandler implementations are resolved from DI.
    /// </summary>
    private class InlineFallbackHandler : IIntentHandler
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
}
