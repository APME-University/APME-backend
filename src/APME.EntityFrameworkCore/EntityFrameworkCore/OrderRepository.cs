using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using APME.Orders;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace APME.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of IOrderRepository
/// </summary>
public class OrderRepository : EfCoreRepository<APMEDbContext, Order, Guid>, IOrderRepository
{
    public OrderRepository(IDbContextProvider<APMEDbContext> dbContextProvider) 
        : base(dbContextProvider)
    {
    }

    public async Task<Order?> GetWithItemsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        return await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        bool includeItems = true,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        var query = dbContext.Orders
            .Where(o => o.OrderNumber == orderNumber.ToUpperInvariant());

        if (includeItems)
        {
            query = query.Include(o => o.Items);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Order>> GetCustomerOrdersAsync(
        Guid customerId,
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        var query = dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(sorting))
        {
            query = query.OrderBy(sorting);
        }
        else
        {
            query = query.OrderByDescending(o => o.CreationTime);
        }

        return await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetCustomerOrderCountAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        return await dbContext.Orders
            .CountAsync(o => o.CustomerId == customerId, cancellationToken);
    }

    public async Task<string> GetNextOrderNumberAsync(
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        var year = DateTime.UtcNow.Year;
        var prefix = $"ORD-{year}-";

        // Get the highest order number for this year (global sequence)
        var lastOrderNumber = await dbContext.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSequence = 1;
        
        if (!string.IsNullOrEmpty(lastOrderNumber))
        {
            // Extract the sequence number from the last order number
            var sequencePart = lastOrderNumber.Replace(prefix, "");
            if (int.TryParse(sequencePart, out int lastSequence))
            {
                nextSequence = lastSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D6}";
    }

    public async Task<List<Order>> GetOrdersContainingShopAsync(
        Guid shopId,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        // Query orders that contain items from the specified shop
        var query = dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.Items.Any(i => i.ShopId == shopId));

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreationTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreationTime <= toDate.Value);
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(sorting))
        {
            query = query.OrderBy(sorting);
        }
        else
        {
            query = query.OrderByDescending(o => o.CreationTime);
        }

        return await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetOrderCountContainingShopAsync(
        Guid shopId,
        OrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        
        // Query orders that contain items from the specified shop
        var query = dbContext.Orders
            .Where(o => o.Items.Any(i => i.ShopId == shopId));

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreationTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreationTime <= toDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public override async Task<IQueryable<Order>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).Include(o => o.Items);
    }
}

