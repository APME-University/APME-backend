namespace APME.Chatbot.Intents;

public class ExtractedEntity
{
    public string Type  { get; init; } = string.Empty;
    // Allowed types (enforce in prompt, not in code):
    // product_name | brand | category | attribute | price_range
    // color | size | use_case | order_number | policy_topic

    public string Value { get; init; } = string.Empty;
}
