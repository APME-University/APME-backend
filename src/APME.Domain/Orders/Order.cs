using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace APME.Orders;

/// <summary>
/// Order Aggregate Root
/// Represents a customer's order - immutable after placement
/// Uses ConcurrencyStamp inherited from AggregateRoot for optimistic concurrency
/// Host-level entity: can contain items from multiple shops
/// </summary>
public class Order : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The customer who placed the order
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Human-readable order number (e.g., ORD-2024-000001)
    /// Sequential globally
    /// </summary>
    public string OrderNumber { get; private set; }

    /// <summary>
    /// Current status of the order
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Shipping address for the order
    /// </summary>
    public Address ShippingAddress { get; private set; }

    /// <summary>
    /// Billing address (null if same as shipping)
    /// </summary>
    public Address? BillingAddress { get; private set; }

    /// <summary>
    /// Payment information snapshot
    /// </summary>
    public PaymentSnapshot Payment { get; private set; }

    /// <summary>
    /// Order line items
    /// </summary>
    public ICollection<OrderItem> Items { get; private set; }

    /// <summary>
    /// Subtotal before tax and shipping
    /// </summary>
    public decimal SubTotal { get; private set; }

    /// <summary>
    /// Total tax amount
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Shipping cost
    /// </summary>
    public decimal ShippingAmount { get; private set; }

    /// <summary>
    /// Total discount amount
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Final total amount (subtotal + tax + shipping - discount)
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Customer notes for the order
    /// </summary>
    public string? CustomerNotes { get; private set; }

    /// <summary>
    /// Internal notes (admin only)
    /// </summary>
    public string? InternalNotes { get; private set; }

    /// <summary>
    /// Tracking number for shipment
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// Carrier name for shipment
    /// </summary>
    public string? ShippingCarrier { get; private set; }

    /// <summary>
    /// When the order was shipped
    /// </summary>
    public DateTime? ShippedAt { get; private set; }

    /// <summary>
    /// When the order was delivered
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>
    /// When the order was cancelled
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public string? CancellationReason { get; private set; }

    protected Order()
    {
        Items = new List<OrderItem>();
    }

    public Order(
        Guid id,
        Guid customerId,
        string orderNumber,
        Address shippingAddress,
        PaymentSnapshot payment,
        string currency,
        Address? billingAddress = null,
        string? customerNotes = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required", nameof(orderNumber));
        if (shippingAddress == null)
            throw new ArgumentNullException(nameof(shippingAddress));
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        CustomerId = customerId;
        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress;
        Payment = payment;
        Currency = currency.Trim().ToUpperInvariant();
        CustomerNotes = customerNotes?.Trim();
        Status = OrderStatus.Pending;
        Items = new List<OrderItem>();
    }

    /// <summary>
    /// Adds an item to the order (only during creation)
    /// </summary>
    public OrderItem AddItem(
        Guid shopId,
        Guid productId,
        string productName,
        string productSku,
        int quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        string? productImageUrl = null)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new BusinessException("APME:CannotModifyOrder")
                .WithData("OrderId", Id)
                .WithData("Status", Status);
        }

        var item = new OrderItem(
            Guid.NewGuid(),
            Id,
            shopId,
            productId,
            productName,
            productSku,
            quantity,
            unitPrice,
            discountAmount,
            taxAmount,
            productImageUrl);

        Items.Add(item);
        RecalculateTotals();
        
        return item;
    }

    /// <summary>
    /// Gets distinct shop IDs from all order items
    /// </summary>
    public IEnumerable<Guid> GetShopIds()
    {
        return Items.Select(x => x.ShopId).Distinct();
    }

    /// <summary>
    /// Groups order items by shop
    /// </summary>
    public IEnumerable<IGrouping<Guid, OrderItem>> GetItemsByShop()
    {
        return Items.GroupBy(x => x.ShopId);
    }

    /// <summary>
    /// Gets the number of distinct shops in this order
    /// </summary>
    public int GetShopCount()
    {
        return Items.Select(x => x.ShopId).Distinct().Count();
    }

    /// <summary>
    /// Sets shipping details
    /// </summary>
    public void SetShippingAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Shipping amount cannot be negative", nameof(amount));

        ShippingAmount = amount;
        RecalculateTotals();
    }

    /// <summary>
    /// Sets discount amount
    /// </summary>
    public void SetDiscountAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Discount amount cannot be negative", nameof(amount));

        DiscountAmount = amount;
        RecalculateTotals();
    }

    /// <summary>
    /// Confirms payment and moves order to processing
    /// </summary>
    public void ConfirmPayment()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new BusinessException("APME:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", OrderStatus.PaymentConfirmed);
        }

        Status = OrderStatus.PaymentConfirmed;
    }

    /// <summary>
    /// Marks order as processing
    /// </summary>
    public void StartProcessing()
    {
        if (Status != OrderStatus.PaymentConfirmed)
        {
            throw new BusinessException("APME:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", OrderStatus.Processing);
        }

        Status = OrderStatus.Processing;
    }

    /// <summary>
    /// Marks order as shipped
    /// </summary>
    public void MarkAsShipped(string? trackingNumber = null, string? carrier = null)
    {
        if (Status != OrderStatus.Processing && Status != OrderStatus.PaymentConfirmed)
        {
            throw new BusinessException("APME:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", OrderStatus.Shipped);
        }

        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber?.Trim();
        ShippingCarrier = carrier?.Trim();
        ShippedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks order as delivered
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new BusinessException("APME:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", OrderStatus.Delivered);
        }

        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the order
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Refunded)
        {
            throw new BusinessException("APME:CannotCancelOrder")
                .WithData("OrderId", Id)
                .WithData("Status", Status);
        }

        Status = OrderStatus.Cancelled;
        CancellationReason = reason?.Trim();
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks order as refunded
    /// </summary>
    public void MarkAsRefunded()
    {
        if (Status != OrderStatus.Cancelled && Status != OrderStatus.Delivered)
        {
            throw new BusinessException("APME:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", OrderStatus.Refunded);
        }

        Status = OrderStatus.Refunded;
    }

    /// <summary>
    /// Marks order as payment failed
    /// </summary>
    public void MarkPaymentFailed()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new BusinessException("APME:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", OrderStatus.PaymentFailed);
        }

        Status = OrderStatus.PaymentFailed;
    }

    /// <summary>
    /// Sets internal notes (admin only)
    /// </summary>
    public void SetInternalNotes(string? notes)
    {
        InternalNotes = notes?.Trim();
    }

    /// <summary>
    /// Updates tracking information
    /// </summary>
    public void UpdateTracking(string trackingNumber, string? carrier = null)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number is required", nameof(trackingNumber));

        TrackingNumber = trackingNumber.Trim();
        ShippingCarrier = carrier?.Trim();
    }

    /// <summary>
    /// Gets the total number of items in the order
    /// </summary>
    public int GetTotalItemCount()
    {
        return Items.Sum(x => x.Quantity);
    }

    /// <summary>
    /// Checks if the order can be cancelled
    /// </summary>
    public bool CanBeCancelled()
    {
        return Status != OrderStatus.Delivered 
               && Status != OrderStatus.Refunded 
               && Status != OrderStatus.Cancelled;
    }

    /// <summary>
    /// Checks if the order can be refunded
    /// </summary>
    public bool CanBeRefunded()
    {
        return Status == OrderStatus.Cancelled || Status == OrderStatus.Delivered;
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(x => x.GetSubTotal());
        TaxAmount = Items.Sum(x => x.TaxAmount);
        TotalAmount = SubTotal + TaxAmount + ShippingAmount - DiscountAmount;
    }
}

