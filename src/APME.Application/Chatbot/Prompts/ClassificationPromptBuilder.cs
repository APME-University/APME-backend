using System.Text;
using APME.Chatbot.Intents;

namespace APME.Chatbot.Prompts;

public static class ClassificationPromptBuilder
{
    public static string BuildSystemPrompt()
    {
        return """
You are an intent-classification engine for an e-commerce chatbot.
Given the user's current message and recent conversation context, you must:

1. Classify the intent into exactly one of these categories:
   - OffTopic        → greeting, chitchat, unrelated question
   - ProductSearch   → user wants to find/discover products
   - ProductComparison → user wants to compare two or more products
   - ProductDetails  → user asks about a specific product's specs
   - Recommendation  → user wants personalised suggestions
   - CategoryBrowse  → user wants to explore a category
   - OrderStatus     → user asks about their order
   - Policy          → shipping, returns, warranty, payment policy
   - AccountManagement → account settings, login, password
   - Unknown         → cannot determine intent

2. Extract named entities from the message. Allowed entity types:
   product_name, brand, category, attribute, price_range,
   color, size, use_case, order_number, policy_topic

3. If the intent is ambiguous or could be multiple intents, set
   requires_clarification=true and provide a clarification_question.

4. If the intent is ProductSearch, also provide a rewritten_query:
   a keyword-optimised version of the user's natural-language query
   suitable for vector/semantic search.

Respond ONLY with valid JSON in this exact schema:
{
  "intent": "<IntentType>",
  "confidence": 0.0-1.0,
  "entities": [{"type":"<entity_type>","value":"<extracted_value>"}],
  "requires_clarification": false,
  "clarification_question": null,
  "rewritten_query": null
}

Do not include any other text, explanation, or markdown.
""";
    }

    public static string BuildUserPrompt(ClassificationContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("### Conversation Context");

        if (context.RecentTurns.Count > 0)
        {
            sb.AppendLine("Recent turns:");
            foreach (var (role, content) in context.RecentTurns)
                sb.AppendLine($"  [{role}]: {content}");
        }

        if (context.LastIntent is not null)
            sb.AppendLine($"Last classified intent: {context.LastIntent}");

        if (context.LastViewedProductIds.Count > 0)
            sb.AppendLine($"Recently viewed product IDs: [{string.Join(", ", context.LastViewedProductIds)}]");

        if (context.CompareList.Count > 0)
            sb.AppendLine($"Current compare list IDs: [{string.Join(", ", context.CompareList)}]");

        if (context.ActiveCategorySlug is not null)
            sb.AppendLine($"Active category: {context.ActiveCategorySlug}");

        sb.AppendLine();
        sb.AppendLine("### Current User Message");
        sb.AppendLine(context.CurrentMessage);

        return sb.ToString();
    }
}
