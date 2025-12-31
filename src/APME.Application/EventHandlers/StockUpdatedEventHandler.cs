using System;
using System.Threading.Tasks;
using APME.Events;
using APME.Products;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;

namespace APME.EventHandlers;

/// <summary>
/// Handler for StockUpdatedEto events
/// Responsibilities: Check for low stock, publish InventoryLowEto if needed
/// </summary>
public class StockUpdatedEventHandler : IDistributedEventHandler<StockUpdatedEto>, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IDistributedEventBus _eventBus;
    private readonly ILogger<StockUpdatedEventHandler> _logger;

    public StockUpdatedEventHandler(
        IRepository<Product, Guid> productRepository,
        IDistributedEventBus eventBus,
        ILogger<StockUpdatedEventHandler> logger)
    {
        _productRepository = productRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleEventAsync(StockUpdatedEto eventData)
    {
        _logger.LogInformation(
            "Stock updated for product {ProductId} ({ProductName}): {OldQty} -> {NewQty} (Reason: {Reason})",
            eventData.ProductId,
            eventData.ProductName,
            eventData.OldQuantity,
            eventData.NewQuantity,
            eventData.Reason);

        // Get the product to check threshold
        var product = await _productRepository.FindAsync(eventData.ProductId);
        
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for stock check", eventData.ProductId);
            return;
        }

        // Check if stock is low or out
        var severity = DetermineAlertSeverity(eventData.NewQuantity, product.LowStockThreshold);
        
        if (severity.HasValue)
        {
            _logger.LogWarning(
                "Low stock alert for product {ProductId} ({ProductName}): {CurrentQty} units (threshold: {Threshold}), severity: {Severity}",
                eventData.ProductId,
                eventData.ProductName,
                eventData.NewQuantity,
                product.LowStockThreshold,
                severity);

            // Publish low inventory event
            await _eventBus.PublishAsync(new InventoryLowEto
            {
                ProductId = eventData.ProductId,
                ShopId = eventData.ShopId,
                TenantId = eventData.TenantId,
                ProductName = eventData.ProductName,
                ProductSku = eventData.ProductSku,
                CurrentQuantity = eventData.NewQuantity,
                ThresholdQuantity = product.LowStockThreshold,
                Severity = severity.Value,
                AlertedAt = DateTime.UtcNow,
                ProductImageUrl = product.PrimaryImageUrl
            });
        }

        // TODO: Invalidate product cache
        _logger.LogDebug("Invalidating cache for product {ProductId}", eventData.ProductId);
        // await _cacheService.InvalidateProductCacheAsync(eventData.ProductId);
    }

    private InventoryAlertSeverity? DetermineAlertSeverity(int currentQuantity, int threshold)
    {
        if (currentQuantity == 0)
        {
            return InventoryAlertSeverity.OutOfStock;
        }
        
        if (currentQuantity <= threshold / 2)
        {
            return InventoryAlertSeverity.Critical;
        }
        
        if (currentQuantity <= threshold)
        {
            return InventoryAlertSeverity.Low;
        }

        return null; // Stock is above threshold
    }
}

