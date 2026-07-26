namespace APME.Settings;

public static class APMESettings
{
    private const string Prefix = "APME";

    // Chatbot settings
    public const string ChatbotClassificationModel = Prefix + ".Chatbot.ClassificationModel";
    public const string ChatbotConfidenceThreshold = Prefix + ".Chatbot.ConfidenceThreshold";
    public const string ChatbotMaxRecentTurns = Prefix + ".Chatbot.MaxRecentTurns";
    public const string ChatbotEnableClassification = Prefix + ".Chatbot.EnableClassification";
}
