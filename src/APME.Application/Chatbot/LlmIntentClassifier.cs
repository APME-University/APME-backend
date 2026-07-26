using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using APME.Chat;
using APME.Chatbot.Intents;
using APME.Chatbot.Prompts;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace APME.Chatbot;

public class LlmIntentClassifier : IIntentClassifier, ITransientDependency
{
    private readonly ILlmProvider _llmProvider;
    private readonly ISettingProvider _settings;
    private readonly ILogger<LlmIntentClassifier> _logger;

    public LlmIntentClassifier(
        ILlmProvider llmProvider,
        ISettingProvider settings,
        ILogger<LlmIntentClassifier> logger)
    {
        _llmProvider = llmProvider;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IntentClassificationResult> ClassifyAsync(
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var enabled = await _settings.GetAsync<bool>(
            Settings.APMESettings.ChatbotEnableClassification);
        if (!enabled)
        {
            _logger.LogWarning("Intent classification disabled via settings");
            return new IntentClassificationResult
            {
                Intent = IntentType.Unknown,
                Confidence = 0f
            };
        }

        var systemPrompt = ClassificationPromptBuilder.BuildSystemPrompt();
        var userPrompt = ClassificationPromptBuilder.BuildUserPrompt(context);

        var request = new GenerationRequest
        {
            SystemPrompt = systemPrompt,
            Stream = false,
            Temperature = 0.1f, // Low temperature for deterministic classification
            MaxTokens = 512
        };
        request.Messages.Add(new GenerationMessage
        {
            Role = "user",
            Content = userPrompt
        });

        try
        {
            var response = await _llmProvider.GenerateResponseAsync(request, ct);
            return ParseLlmResponse(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM intent classification failed");
            return new IntentClassificationResult
            {
                Intent = IntentType.Unknown,
                Confidence = 0f
            };
        }
    }

    private IntentClassificationResult ParseLlmResponse(string raw)
    {
        _logger.LogInformation("LLM classification raw response: {Raw}", raw);

        // Strip markdown code fences if present
        var json = raw.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            if (firstNewline >= 0) json = json[(firstNewline + 1)..];
            if (json.EndsWith("```")) json = json[..^3];
            json = json.Trim();
        }

        // Extract JSON object from surrounding text (LLM may add conversational wrapper)
        json = ExtractJsonObject(json);

        if (json == null)
        {
            _logger.LogWarning("No JSON object found in LLM response, falling back to Unknown intent");
            return new IntentClassificationResult
            {
                Intent = IntentType.Unknown,
                Confidence = 0f
            };
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var intentStr = root.GetProperty("intent").GetString() ?? "Unknown";
            if (!Enum.TryParse<IntentType>(intentStr, ignoreCase: true, out var intent))
                intent = IntentType.Unknown;

            var confidence = root.TryGetProperty("confidence", out var confEl)
                ? confEl.GetSingle()
                : 0f;

            var entities = new List<ExtractedEntity>();
            if (root.TryGetProperty("entities", out var entitiesEl))
            {
                foreach (var e in entitiesEl.EnumerateArray())
                {
                    entities.Add(new ExtractedEntity
                    {
                        Type = e.GetProperty("type").GetString() ?? string.Empty,
                        Value = e.GetProperty("value").GetString() ?? string.Empty
                    });
                }
            }

            var requiresClarification = root.TryGetProperty("requires_clarification", out var rc)
                && rc.GetBoolean();

            var clarificationQuestion = root.TryGetProperty("clarification_question", out var cq)
                && cq.ValueKind == JsonValueKind.String
                ? cq.GetString()
                : null;

            var rewrittenQuery = root.TryGetProperty("rewritten_query", out var rq)
                && rq.ValueKind == JsonValueKind.String
                ? rq.GetString()
                : null;

            return new IntentClassificationResult
            {
                Intent = intent,
                Confidence = confidence,
                Entities = entities,
                RequiresClarification = requiresClarification,
                ClarificationQuestion = clarificationQuestion,
                RewrittenQuery = rewrittenQuery
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM classification JSON: {Raw}", raw);
            return new IntentClassificationResult
            {
                Intent = IntentType.Unknown,
                Confidence = 0f
            };
        }
    }

    /// <summary>
    /// Extracts the first valid JSON object {...} from text that may contain
    /// conversational wrapper text before/after the JSON.
    /// </summary>
    private string? ExtractJsonObject(string text)
    {
        // Find the first '{' and match its closing '}'
        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return text.Substring(start, i - start + 1);
            }
        }

        // Unclosed brace — return everything from start as a best effort
        return text.Substring(start);
    }
}
