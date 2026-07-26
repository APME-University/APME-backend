using System;
using System.Collections.Generic;
using System.Linq;
using OllamaRequest = OllamaSharp.Models.Chat.ChatRequest;
using OllamaMessage = OllamaSharp.Models.Chat.Message;
using OllamaChatRole = OllamaSharp.Models.Chat.ChatRole;
using OllamaRequestOptions = OllamaSharp.Models.RequestOptions;

namespace APME.AI.Adapters;

/// <summary>
/// Adapter for converting domain GenerationRequest to OllamaSharp ChatRequest.
/// </summary>
public static class GenerationRequestAdapter
{
    /// <summary>
    /// Converts a domain GenerationRequest to an OllamaSharp ChatRequest.
    /// </summary>
    /// <param name="request">The domain request.</param>
    /// <param name="model">The model name to use.</param>
    /// <param name="temperature">Temperature setting.</param>
    /// <param name="maxTokens">Maximum tokens to generate.</param>
    /// <returns>OllamaSharp ChatRequest.</returns>
    public static OllamaRequest ToOllamaChatRequest(
        GenerationRequest request,
        string model,
        float temperature,
        int maxTokens)
    {
        var messages = new List<OllamaMessage>();

        // Prepend system prompt if provided
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new OllamaMessage
            {
                Role = OllamaChatRole.System,
                Content = request.SystemPrompt
            });
        }

        foreach (var msg in request.Messages)
        {
            var role = msg.Role.ToLowerInvariant() switch
            {
                "system" => OllamaChatRole.System,
                "assistant" => OllamaChatRole.Assistant,
                "user" => OllamaChatRole.User,
                _ => OllamaChatRole.User
            };

            var message = new OllamaMessage
            {
                Role = role,
                Content = msg.Content
            };

            if (msg.Images?.Any() == true)
            {
                message.Images = msg.Images.ToArray();
            }

            messages.Add(message);
        }

        return new OllamaRequest
        {
            Model = model,
            Messages = messages,
            Stream = request.Stream,
            Options = new OllamaRequestOptions
            {
                Temperature = request.Temperature > 0 ? request.Temperature : temperature,
                NumPredict = request.MaxTokens > 0 ? request.MaxTokens : maxTokens
            }
        };
    }

    /// <summary>
    /// Converts domain ChatMessage list to OllamaSharp Message list.
    /// </summary>
    public static List<OllamaMessage> ToOllamaMessages(List<ChatMessage> messages)
    {
        return messages.Select(msg => new OllamaMessage
        {
            Role = msg.Role.ToLowerInvariant() switch
            {
                "system" => OllamaChatRole.System,
                "assistant" => OllamaChatRole.Assistant,
                "user" => OllamaChatRole.User,
                _ => OllamaChatRole.User
            },
            Content = msg.Content
        }).ToList();
    }

}
