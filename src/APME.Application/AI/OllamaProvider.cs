using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APME.AI.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Volo.Abp.DependencyInjection;

namespace APME.AI;

/// <summary>
/// Ollama implementation of ILlmProvider.
/// Anti-corruption layer that isolates all OllamaSharp-specific code.
/// </summary>
public class OllamaProvider : ILlmProvider, ITransientDependency
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly AIOptions _options;
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(
        IOptions<AIOptions> options,
        ILogger<OllamaProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        var uri = new Uri(_options.OllamaBaseUrl);
        _ollamaClient = new OllamaApiClient(uri);
        _ollamaClient.SelectedModel = _options.GenerationModel;
    }

    /// <inheritdoc />
    public string ModelName => _options.GenerationModel;

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        GenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Starting streaming generation with model {Model}, {MessageCount} messages",
            _options.GenerationModel,
            request.Messages.Count);

        var chatRequest = GenerationRequestAdapter.ToOllamaChatRequest(
            request,
            _options.GenerationModel,
            _options.GenerationTemperature,
            _options.MaxGenerationTokens);

        await foreach (var response in _ollamaClient.ChatAsync(chatRequest, cancellationToken))
        {
            if (!string.IsNullOrEmpty(response?.Message?.Content))
            {
                yield return response.Message.Content;
            }
        }
    }

    /// <inheritdoc />
    public async Task<GenerationResponse> GenerateResponseAsync(
        GenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Starting non-streaming generation with model {Model}",
            _options.GenerationModel);

        var chatRequest = GenerationRequestAdapter.ToOllamaChatRequest(
            request,
            _options.GenerationModel,
            _options.GenerationTemperature,
            _options.MaxGenerationTokens);

        var responseBuilder = new StringBuilder();

        await foreach (var response in _ollamaClient.ChatAsync(chatRequest, cancellationToken))
        {
            if (!string.IsNullOrEmpty(response?.Message?.Content))
            {
                responseBuilder.Append(response.Message.Content);
            }
        }

        stopwatch.Stop();

        var generationResponse = new GenerationResponse
        {
            Content = responseBuilder.ToString(),
            ModelUsed = _options.GenerationModel,
            GenerationTimeMs = stopwatch.ElapsedMilliseconds,
            RequestId = Guid.NewGuid().ToString(),
            FinishReason = "stop"
        };

        _logger.LogDebug(
            "Generation completed in {TimeMs}ms, {ContentLength} chars",
            stopwatch.ElapsedMilliseconds,
            generationResponse.Content.Length);

        return generationResponse;
    }

    /// <inheritdoc />
    public async Task<ModelInfo> GetModelInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await _ollamaClient.ListLocalModelsAsync(cancellationToken);
            var targetModel = models.FirstOrDefault(m =>
                m.Name.Contains(_options.GenerationModel, StringComparison.OrdinalIgnoreCase));

            if (targetModel == null)
            {
                _logger.LogWarning(
                    "Model {Model} not found in local models",
                    _options.GenerationModel);

                return new ModelInfo
                {
                    Name = _options.GenerationModel,
                    IsAvailable = false
                };
            }

            return new ModelInfo
            {
                Name = targetModel.Name,
                Size = targetModel.Size,
                ModifiedAt = targetModel.ModifiedAt,
                Digest = targetModel.Digest,
                IsAvailable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model info for {Model}", _options.GenerationModel);
            return new ModelInfo
            {
                Name = _options.GenerationModel,
                IsAvailable = false
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await _ollamaClient.ListLocalModelsAsync(cancellationToken);
            var hasModel = models.Any(m =>
                m.Name.Contains(_options.GenerationModel, StringComparison.OrdinalIgnoreCase));

            _logger.LogDebug(
                "Ollama connection test: {Status}, model available: {HasModel}",
                "Success",
                hasModel);

            return hasModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama connection test failed");
            return false;
        }
    }
}
