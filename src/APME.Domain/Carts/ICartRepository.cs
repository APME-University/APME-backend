using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace APME.Carts;

/// <summary>
/// Custom repository interface for Cart aggregate
/// Cart is at host level (not tenant-specific) - one active cart per customer
/// </summary>
public interface ICartRepository : IRepository<Cart, Guid>
{
    /// <summary>
    /// Gets the active cart for a customer (host level, not tenant-specific)
    /// </summary>
    Task<Cart?> GetActiveCartAsync(
        Guid customerId,
        bool includeItems = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart by ID with items included
    /// </summary>
    Task<Cart?> GetWithItemsAsync(
        Guid cartId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer has an active cart
    /// </summary>
    Task<bool> HasActiveCartAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart by cart item ID
    /// </summary>
    Task<Cart?> GetByCartItemIdAsync(
        Guid customerId,
        Guid cartItemId,
        CancellationToken cancellationToken = default);
}
