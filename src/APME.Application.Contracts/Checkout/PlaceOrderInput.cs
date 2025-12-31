using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Checkout;

/// <summary>
/// Input for placing an order after payment
/// FR7.1 - Place order
/// </summary>
public class PlaceOrderInput
{
    /// <summary>
    /// The Stripe PaymentIntent ID
    /// </summary>
    [Required]
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Shipping address for the order
    /// </summary>
    [Required]
    public AddressDto ShippingAddress { get; set; } = null!;

    /// <summary>
    /// Billing address (optional, uses shipping if not provided)
    /// </summary>
    public AddressDto? BillingAddress { get; set; }

    /// <summary>
    /// Customer notes for the order
    /// </summary>
    [MaxLength(1000)]
    public string? CustomerNotes { get; set; }
}

/// <summary>
/// Result of placing an order
/// </summary>
public class PlaceOrderResult
{
    /// <summary>
    /// Whether the order was placed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The created order ID
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// The order number
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// Error message if order failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for specific error handling
    /// </summary>
    public PlaceOrderErrorCode? ErrorCode { get; set; }
}

/// <summary>
/// Error codes for order placement
/// </summary>
public enum PlaceOrderErrorCode
{
    Unknown,
    EmptyCart,
    PaymentFailed,
    PaymentNotConfirmed,
    InsufficientStock,
    ProductNotAvailable,
    ConcurrencyConflict,
    InvalidAddress
}
