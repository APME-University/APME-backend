using APME.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace APME.Settings;

public class APMESettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                APMESettings.ChatbotClassificationModel,
                "llama3.2:latest",
                L("DisplayName:Chatbot.ClassificationModel"),
                L("Description:Chatbot.ClassificationModel")
            ),
            new SettingDefinition(
                APMESettings.ChatbotConfidenceThreshold,
                "0.65",
                L("DisplayName:Chatbot.ConfidenceThreshold"),
                L("Description:Chatbot.ConfidenceThreshold")
            ),
            new SettingDefinition(
                APMESettings.ChatbotMaxRecentTurns,
                "6",
                L("DisplayName:Chatbot.MaxRecentTurns"),
                L("Description:Chatbot.MaxRecentTurns")
            ),
            new SettingDefinition(
                APMESettings.ChatbotEnableClassification,
                "true",
                L("DisplayName:Chatbot.EnableClassification"),
                L("Description:Chatbot.EnableClassification")
            )
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create(name);
    }
}
