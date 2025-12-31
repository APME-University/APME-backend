using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Carts;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace APME.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of ICartRepository
/// Cart is at host level - no tenant filtering
/// </summary>
public class CartRepository : EfCoreRepository<APMEDbContext, Cart, Guid>, ICartRepository
{
    public CartRepository(IDbContextProvider<APMEDbContext> dbContextProvider) 
        : base(dbContextProvider)
    {
    }

    public async Task<Cart?> GetActiveCartAsync(
        Guid customerId,
        bool includeItems = true,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        var query = dbContext.Carts
            .Where(c => c.CustomerId == customerId && c.Status == CartStatus.Active);

        if (includeItems)
        {
            query = query.Include(c => c.Items);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cart?> GetWithItemsAsync(
        Guid cartId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        return await dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
    }

    public async Task<bool> HasActiveCartAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        return await dbContext.Carts
            .AnyAsync(c => c.CustomerId == customerId && c.Status == CartStatus.Active, 
                cancellationToken);
    }

    public async Task<Cart?> GetByCartItemIdAsync(
        Guid customerId,
        Guid cartItemId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        return await dbContext.Carts
            .Include(c => c.Items)
            .Where(c => c.CustomerId == customerId 
                        && c.Status == CartStatus.Active
                        && c.Items.Any(i => i.Id == cartItemId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<IQueryable<Cart>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).Include(c => c.Items);
    }
}
