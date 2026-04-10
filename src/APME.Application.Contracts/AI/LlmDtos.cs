using System;
using System.Collections.Generic;

namespace APME.AI;

/// <summary>
/// Request for LLM text generation.
/// </summary>
public class GenerationRequest
{
    /// <summary>
    /// The messages to send to the LLM.
    /// </summary>
    public List<GenerationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Temperature for generation (0.0 = deterministic, 1.0 = creative).
    /// </summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// Top-p sampling parameter.
    /// </summary>
    public float TopP { get; set; } = 0.9f;

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; set; } = true;

    /// <summary>
    /// Optional system prompt override.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Session ID for tracking.
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// A message in the generation request.
/// </summary>
public class GenerationMessage
{
    /// <summary>
    /// The role: "system", "user", or "assistant".
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// The message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional images (base64 encoded) for multimodal models.
    /// </summary>
    public List<string>? Images { get; set; }

    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response from LLM generation.
/// </summary>
public class GenerationResponse
{
    /// <summary>
    /// The generated content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Token usage information.
    /// </summary>
    public GenerationTokenUsage? TokenUsage { get; set; }

    /// <summary>
    /// Time taken to generate in milliseconds.
    /// </summary>
    public long GenerationTimeMs { get; set; }

    /// <summary>
    /// The model used for generation.
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    /// Request ID for tracking.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Finish reason (stop, length, etc.).
    /// </summary>
    public string? FinishReason { get; set; }
}

/// <summary>
/// Token usage statistics for a generation.
/// </summary>
public class GenerationTokenUsage
{
    /// <summary>
    /// Tokens used for the prompt.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Tokens generated in the response.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Information about an LLM model.
/// </summary>
public class ModelInfo
{
    /// <summary>
    /// The model name/identifier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// When the model was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Model digest/hash for version tracking.
    /// </summary>
    public string? Digest { get; set; }

    /// <summary>
    /// Model family (llama, mistral, etc.).
    /// </summary>
    public string? Family { get; set; }

    /// <summary>
    /// Parameter count (e.g., "7B", "13B").
    /// </summary>
    public string? ParameterSize { get; set; }

    /// <summary>
    /// Quantization level (e.g., "Q4_0", "Q8_0").
    /// </summary>
    public string? QuantizationLevel { get; set; }

    /// <summary>
    /// Whether the model is available/loaded.
    /// </summary>
    public bool IsAvailable { get; set; }
}
