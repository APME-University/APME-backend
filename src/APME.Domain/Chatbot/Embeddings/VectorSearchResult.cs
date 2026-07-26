using System;

namespace APME.Chatbot.Embeddings;

public class VectorSearchResult
{
    public Guid ProductId { get; init; }

    /// <summary>
    /// Cosine similarity score (0.0 – 1.0) from vector search.
    /// </summary>
    public float SimilarityScore { get; init; }

    /// <summary>
    /// Final score after fusion with popularity signal.
    /// Populated by ScoreFusionRanker — not set by IVectorStore directly.
    /// </summary>
    public float FinalScore { get; set; }
}
