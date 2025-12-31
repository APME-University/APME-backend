using System.Threading.Tasks;
using APME.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace APME.EventHandlers;

/// <summary>
/// Handler for InventoryLowEto events (FR14.5)
/// Responsibilities: Notify shop admin, trigger reorder alerts
/// </summary>
public class InventoryLowEventHandler : IDistributedEventHandler<InventoryLowEto>, ITransientDependency
{
    private readonly ILogger<InventoryLowEventHandler> _logger;
    // TODO: Inject notification service, email service

    public InventoryLowEventHandler(ILogger<InventoryLowEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleEventAsync(InventoryLowEto eventData)
    {
        var severityLabel = eventData.Severity switch
        {
            InventoryAlertSeverity.OutOfStock => "OUT OF STOCK",
            InventoryAlertSeverity.Critical => "CRITICAL",
            InventoryAlertSeverity.Low => "LOW",
            _ => "UNKNOWN"
        };

        _logger.LogWarning(
            "[{Severity}] Inventory alert for {ProductName} (SKU: {Sku}) in shop {ShopId}: {CurrentQty} units remaining (threshold: {Threshold})",
            severityLabel,
            eventData.ProductName,
            eventData.ProductSku,
            eventData.ShopId,
            eventData.CurrentQuantity,
            eventData.ThresholdQuantity);

        // TODO: Send notification to shop admin
        _logger.LogInformation(
            "Sending inventory alert notification to shop {ShopId} admin",
            eventData.ShopId);
        // await _notificationService.SendInventoryAlertAsync(eventData);

        // TODO: Send email to shop admin
        // await _emailService.SendInventoryAlertEmailAsync(eventData);

        // TODO: For out of stock, might want to:
        // - Hide product from storefront
        // - Send notification to customers with this item in cart
        if (eventData.Severity == InventoryAlertSeverity.OutOfStock)
        {
            _logger.LogWarning(
                "Product {ProductId} is OUT OF STOCK - consider hiding from storefront",
                eventData.ProductId);
            // await _productService.HideOutOfStockProductAsync(eventData.ProductId);
        }

        await Task.CompletedTask;
    }
}

