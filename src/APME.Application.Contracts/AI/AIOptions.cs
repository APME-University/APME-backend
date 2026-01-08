namespace APME.AI;

/// <summary>
/// Configuration options for AI services.
/// SRS Reference: AI Chatbot RAG Architecture - Configuration
/// </summary>
public class AIOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AI:Ollama";

    /// <summary>
    /// Base URL for Ollama API server.
    /// Default: http://localhost:11434
    /// </summary>
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name for generating embeddings.
    /// Default: embeddinggemma (768 dimensions)
    /// </summary>
    public string EmbeddingModel { get; set; } = "embeddinggemma";

    /// <summary>
    /// Model name for text generation (chat).
    /// Default: llama3.2:latest
    /// </summary>
    public string GenerationModel { get; set; } = "llama3.2:latest";

    /// <summary>
    /// Dimension of embedding vectors.
    /// Must match the embedding model output.
    /// Default: 768 (for gemma2)
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 768;

    /// <summary>
    /// Maximum tokens per chunk for embedding.
    /// Long texts are split into chunks to respect model limits.
    /// Default: 512
    /// </summary>
    public int MaxTokensPerChunk { get; set; } = 512;

    /// <summary>
    /// Overlap between chunks (in tokens).
    /// Provides context continuity between chunks.
    /// Default: 50
    /// </summary>
    public int ChunkOverlapTokens { get; set; } = 50;

    /// <summary>
    /// Embedding model version for tracking and bulk re-indexing.
    /// Increment when switching models or model versions.
    /// </summary>
    public int EmbeddingModelVersion { get; set; } = 1;

    /// <summary>
    /// Maximum number of results for semantic search.
    /// Default: 10
    /// </summary>
    public int DefaultTopK { get; set; } = 10;

    /// <summary>
    /// Temperature for text generation (0.0 = deterministic, 1.0 = creative).
    /// Default: 0.7
    /// </summary>
    public float GenerationTemperature { get; set; } = 0.7f;

    /// <summary>
    /// Maximum tokens for generated responses.
    /// Default: 1024
    /// </summary>
    public int MaxGenerationTokens { get; set; } = 1024;

    /// <summary>
    /// Whether to enable embedding generation.
    /// Can be disabled for testing or when Ollama is not available.
    /// </summary>
    public bool EnableEmbeddingGeneration { get; set; } = true;

    /// <summary>
    /// Timeout in seconds for Ollama API calls.
    /// Default: 60
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 60;
}

