using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.AI;

/// <summary>
/// Service for splitting content into chunks suitable for embedding generation.
/// Respects token limits while maintaining semantic coherence.
/// SRS Reference: AI Chatbot RAG Architecture - Content Chunking
/// </summary>
public class ContentChunker : IContentChunker, ITransientDependency
{
    private readonly AIOptions _options;

    // Approximate characters per token (conservative estimate)
    private const double CharsPerToken = 4.0;

    public ContentChunker(IOptions<AIOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public List<ContentChunk> ChunkContent(string content, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<ContentChunk>();
        }

        var chunks = new List<ContentChunk>();
        var maxCharsPerChunk = (int)(_options.MaxTokensPerChunk * CharsPerToken);
        var overlapChars = (int)(_options.ChunkOverlapTokens * CharsPerToken);

        // If content fits in single chunk, return as-is
        if (content.Length <= maxCharsPerChunk)
        {
            chunks.Add(new ContentChunk
            {
                Index = 0,
                Text = PrepareChunkText(content, title, 0, 1),
                StartPosition = 0,
                EndPosition = content.Length,
                IsComplete = true
            });
            return chunks;
        }

        // Split content into chunks with overlap
        var position = 0;
        var chunkIndex = 0;

        while (position < content.Length)
        {
            var remainingLength = content.Length - position;
            var chunkLength = Math.Min(maxCharsPerChunk, remainingLength);

            // Try to break at natural boundaries (sentence, paragraph)
            if (position + chunkLength < content.Length)
            {
                chunkLength = FindNaturalBreak(content, position, chunkLength);
            }

            var chunkText = content.Substring(position, chunkLength);
            var isLastChunk = position + chunkLength >= content.Length;
            var totalChunks = EstimateTotalChunks(content.Length, maxCharsPerChunk, overlapChars);

            chunks.Add(new ContentChunk
            {
                Index = chunkIndex,
                Text = PrepareChunkText(chunkText, title, chunkIndex, totalChunks),
                StartPosition = position,
                EndPosition = position + chunkLength,
                IsComplete = isLastChunk
            });

            // Move position forward (with overlap)
            position += chunkLength - (isLastChunk ? 0 : overlapChars);
            chunkIndex++;
        }

        return chunks;
    }

    /// <inheritdoc />
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    /// <summary>
    /// Finds a natural break point (sentence or paragraph end) near the target position.
    /// </summary>
    private int FindNaturalBreak(string content, int start, int maxLength)
    {
        var searchStart = start + (int)(maxLength * 0.8); // Start looking at 80% of max length
        var searchEnd = start + maxLength;

        // Look for paragraph break first
        var paragraphBreak = content.LastIndexOf("\n\n", searchEnd, searchEnd - searchStart);
        if (paragraphBreak > searchStart)
        {
            return paragraphBreak - start + 2; // Include the newlines
        }

        // Look for sentence end (.!?)
        for (var i = searchEnd - 1; i >= searchStart; i--)
        {
            var c = content[i];
            if (c == '.' || c == '!' || c == '?')
            {
                // Make sure it's followed by whitespace or end of string
                if (i + 1 >= content.Length || char.IsWhiteSpace(content[i + 1]))
                {
                    return i - start + 1;
                }
            }
        }

        // Look for any newline
        var newlineBreak = content.LastIndexOf('\n', searchEnd, searchEnd - searchStart);
        if (newlineBreak > searchStart)
        {
            return newlineBreak - start + 1;
        }

        // Look for space (word boundary)
        var spaceBreak = content.LastIndexOf(' ', searchEnd, searchEnd - searchStart);
        if (spaceBreak > searchStart)
        {
            return spaceBreak - start;
        }

        // No natural break found, use max length
        return maxLength;
    }

    /// <summary>
    /// Prepares chunk text with optional context (title, chunk number).
    /// </summary>
    private string PrepareChunkText(string chunkText, string? title, int chunkIndex, int totalChunks)
    {
        var sb = new StringBuilder();

        // Add title context if provided and this is the first chunk
        if (!string.IsNullOrWhiteSpace(title) && chunkIndex == 0)
        {
            sb.AppendLine($"Title: {title}");
            sb.AppendLine();
        }

        // Add chunk indicator if multi-chunk
        if (totalChunks > 1)
        {
            sb.AppendLine($"[Part {chunkIndex + 1} of {totalChunks}]");
        }

        sb.Append(chunkText.Trim());

        return sb.ToString();
    }

    /// <summary>
    /// Estimates total number of chunks for a given content length.
    /// </summary>
    private int EstimateTotalChunks(int contentLength, int maxCharsPerChunk, int overlapChars)
    {
        if (contentLength <= maxCharsPerChunk)
        {
            return 1;
        }

        var effectiveChunkLength = maxCharsPerChunk - overlapChars;
        return (int)Math.Ceiling((double)(contentLength - overlapChars) / effectiveChunkLength);
    }
}

/// <summary>
/// Interface for content chunking service.
/// </summary>
public interface IContentChunker
{
    /// <summary>
    /// Splits content into chunks suitable for embedding.
    /// </summary>
    /// <param name="content">The content to chunk.</param>
    /// <param name="title">Optional title for context.</param>
    /// <returns>List of content chunks.</returns>
    List<ContentChunk> ChunkContent(string content, string? title = null);

    /// <summary>
    /// Estimates the token count for given text.
    /// </summary>
    int EstimateTokenCount(string text);
}

/// <summary>
/// Represents a chunk of content for embedding.
/// </summary>
public class ContentChunk
{
    /// <summary>
    /// Zero-based index of this chunk.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The chunk text (may include title/context).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Start position in original content.
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// End position in original content.
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Whether this is the last chunk.
    /// </summary>
    public bool IsComplete { get; set; }
}









