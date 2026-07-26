using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;

namespace APME.Chatbot.Embeddings;

/// <summary>
/// Orchestrates: embed query → vector search → hard filter → score fusion → re-rank.
/// This is the ONLY service ProductSearchHandler calls for semantic search.
/// </summary>
public interface IHybridSearchService : IDomainService
{
    Task<IReadOnlyList<Guid>> SearchProductsAsync(
        string query,
        EmbeddingSearchRequest request,
        CancellationToken ct = default);
}
