using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.Checkout;

/// <summary>
/// Application service interface for checkout operations
/// All operations require authenticated customer
/// Cart is multi-shop - one cart can contain items from multiple shops
/// </summary>
public interface ICheckoutAppService : IApplicationService
{
    /// <summary>
    /// Gets the checkout summary for the current cart
    /// Includes validation and totals calculation
    /// FR7.4 - Preview order before confirmation
    /// </summary>
    Task<CheckoutSummaryDto> GetCheckoutSummaryAsync();

    /// <summary>
    /// Creates a Stripe PaymentIntent for the checkout
    /// Returns client secret for frontend payment form
    /// FR7.1 - Initialize payment
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentInput input);

    /// <summary>
    /// Places an order after successful payment
    /// Atomic operation: validates stock, deducts inventory, creates order
    /// FR7.1, UC11 - Complete checkout
    /// </summary>
    Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderInput input);

    /// <summary>
    /// Cancels a pending PaymentIntent (if customer abandons checkout)
    /// </summary>
    Task CancelPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Handles Stripe webhook events (payment confirmation, etc.)
    /// </summary>
    Task HandleStripeWebhookAsync(string payload, string signature);
}
