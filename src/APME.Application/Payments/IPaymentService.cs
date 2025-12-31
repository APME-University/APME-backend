using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APME.Checkout;

namespace APME.Payments;

/// <summary>
/// Interface for payment processing operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a PaymentIntent for the specified amount
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, 
        string currency, 
        Guid orderId,
        string? customerEmail = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Confirms a PaymentIntent
    /// </summary>
    Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId);

    /// <summary>
    /// Cancels a PaymentIntent
    /// </summary>
    Task CancelPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Retrieves a PaymentIntent to check its status
    /// </summary>
    Task<PaymentIntentStatusResult> GetPaymentIntentStatusAsync(string paymentIntentId);

    /// <summary>
    /// Processes a Stripe webhook event
    /// </summary>
    Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string signature);
}

/// <summary>
/// Result of checking PaymentIntent status
/// </summary>
public class PaymentIntentStatusResult
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSucceeded { get; set; }
    public bool IsCanceled { get; set; }
    public bool RequiresAction { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of processing a webhook
/// </summary>
public class WebhookProcessResult
{
    public bool Success { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? ErrorMessage { get; set; }
}

