using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Pgvector;
using Volo.Abp.DependencyInjection;

namespace APME.AI;

/// <summary>
/// Implementation of IOllamaEmbeddingService using OllamaSharp client.
/// Connects to local Ollama server for embedding generation.
/// SRS Reference: AI Chatbot RAG Architecture - Local-first AI with Ollama
/// </summary>
public class OllamaEmbeddingService : IOllamaEmbeddingService, ISingletonDependency
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly AIOptions _options;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public int EmbeddingDimension => _options.EmbeddingDimensions;
    public string ModelName => _options.EmbeddingModel;
    public int ModelVersion => _options.EmbeddingModelVersion;

    public OllamaEmbeddingService(
        IOptions<AIOptions> options,
        ILogger<OllamaEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Initialize Ollama client
        var uri = new Uri(_options.OllamaBaseUrl);
        _ollamaClient = new OllamaApiClient(uri);
        _ollamaClient.SelectedModel = _options.EmbeddingModel;

        _logger.LogInformation(
            "OllamaEmbeddingService initialized with model {Model} at {BaseUrl}",
            _options.EmbeddingModel, _options.OllamaBaseUrl);
    }

    /// <inheritdoc />
    public async Task<Vector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        try
        {
            _logger.LogDebug(
                "Generating embedding for text of length {Length} using model {Model}",
                text.Length, _options.EmbeddingModel);

            var response = await _ollamaClient.EmbedAsync(
                new OllamaSharp.Models.EmbedRequest
                {
                    Model = _options.EmbeddingModel,
                    Input = [text]
                },
                cancellationToken);

            if (response?.Embeddings == null || response.Embeddings.Count == 0)
            {
                throw new InvalidOperationException("Ollama returned empty embedding response");
            }

            var embeddingArray = response.Embeddings[0].ToArray();

            // Validate dimension
            if (embeddingArray.Length != _options.EmbeddingDimensions)
            {
                _logger.LogWarning(
                    "Embedding dimension mismatch: expected {Expected}, got {Actual}",
                    _options.EmbeddingDimensions, embeddingArray.Length);
            }

            // Convert to float array for pgvector
            var floatArray = embeddingArray.Select(d => (float)d).ToArray();
            return new Vector(floatArray);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Failed to generate embedding using model {Model}",
                _options.EmbeddingModel);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<Vector>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        
        if (textList.Count == 0)
        {
            return new List<Vector>();
        }

        try
        {
            _logger.LogDebug(
                "Generating {Count} embeddings using model {Model}",
                textList.Count, _options.EmbeddingModel);

            var response = await _ollamaClient.EmbedAsync(
                new OllamaSharp.Models.EmbedRequest
                {
                    Model = _options.EmbeddingModel,
                    Input = textList
                },
                cancellationToken);

            if (response?.Embeddings == null || response.Embeddings.Count != textList.Count)
            {
                throw new InvalidOperationException(
                    $"Ollama returned {response?.Embeddings?.Count ?? 0} embeddings, expected {textList.Count}");
            }

            var results = new List<Vector>(textList.Count);
            foreach (var embedding in response.Embeddings)
            {
                var floatArray = embedding.Select(d => (float)d).ToArray();
                results.Add(new Vector(floatArray));
            }

            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Failed to generate {Count} embeddings using model {Model}",
                textList.Count, _options.EmbeddingModel);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing Ollama connection at {BaseUrl}", _options.OllamaBaseUrl);

            // Try to list models as a health check
            var models = await _ollamaClient.ListLocalModelsAsync(cancellationToken);
            
            var modelExists = models.Any(m => 
                m.Name.StartsWith(_options.EmbeddingModel, StringComparison.OrdinalIgnoreCase));

            if (!modelExists)
            {
                _logger.LogWarning(
                    "Embedding model {Model} not found. Available models: {Models}",
                    _options.EmbeddingModel,
                    string.Join(", ", models.Select(m => m.Name)));
            }

            _logger.LogInformation(
                "Ollama connection successful. Model {Model} available: {Available}",
                _options.EmbeddingModel, modelExists);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to connect to Ollama at {BaseUrl}",
                _options.OllamaBaseUrl);
            return false;
        }
    }
}








