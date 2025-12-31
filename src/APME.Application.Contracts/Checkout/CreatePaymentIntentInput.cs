using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Checkout;

/// <summary>
/// Input for creating a Stripe PaymentIntent
/// </summary>
public class CreatePaymentIntentInput
{
    /// <summary>
    /// Shipping address for the order
    /// </summary>
    [Required]
    public AddressDto ShippingAddress { get; set; } = null!;

    /// <summary>
    /// Billing address (optional, uses shipping if not provided)
    /// </summary>
    public AddressDto? BillingAddress { get; set; }
}

/// <summary>
/// Result of creating a PaymentIntent
/// </summary>
public class PaymentIntentResult
{
    /// <summary>
    /// The Stripe PaymentIntent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret for Stripe.js
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Amount in smallest currency unit (cents for USD)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Whether the PaymentIntent was created successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
