using System;
using System.Threading;
using System.Threading.Tasks;

namespace APME.Products;

public interface ISearchableTextService
{
    /// <summary>
    /// Builds the SearchableText pipe-delimited string for a product.
    /// Format: "Name | Brand: X | Category: Path | Tags: t1, t2 | Desc: ... | Attrs: key=val; ..."
    /// </summary>
    Task<string> BuildAsync(Guid productId, CancellationToken cancellationToken = default);
}
