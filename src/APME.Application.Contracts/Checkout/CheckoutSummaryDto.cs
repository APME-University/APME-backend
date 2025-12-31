using System;
using System.Collections.Generic;
using APME.Carts;

namespace APME.Checkout;

/// <summary>
/// DTO for checkout summary/preview
/// FR7.4 - Preview order before confirmation
/// </summary>
public class CheckoutSummaryDto
{
    /// <summary>
    /// The cart being checked out
    /// </summary>
    public CartViewDto Cart { get; set; } = null!;

    /// <summary>
    /// Subtotal (sum of line totals)
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Estimated tax amount
    /// </summary>
    public decimal EstimatedTax { get; set; }

    /// <summary>
    /// Shipping cost
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Discount amount
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Total amount to charge
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether the checkout can proceed
    /// </summary>
    public bool CanCheckout { get; set; }

    /// <summary>
    /// Validation errors preventing checkout
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Available shipping options (for future use)
    /// </summary>
    public List<ShippingOptionDto> ShippingOptions { get; set; } = new();
}

/// <summary>
/// DTO for shipping option (placeholder for future shipping integration)
/// </summary>
public class ShippingOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string EstimatedDelivery { get; set; } = string.Empty;
}

