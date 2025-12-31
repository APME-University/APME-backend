using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APME.Checkout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Volo.Abp.DependencyInjection;

namespace APME.Payments;

/// <summary>
/// Stripe implementation of IPaymentService
/// </summary>
public class StripePaymentService : IPaymentService, ITransientDependency
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IOptions<StripeOptions> options,
        ILogger<StripePaymentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid orderId,
        string? customerEmail = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Convert to smallest currency unit (cents for USD)
            var amountInCents = (long)(amount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = currency.ToLowerInvariant(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            if (!string.IsNullOrEmpty(customerEmail))
            {
                options.ReceiptEmail = customerEmail;
            }

            options.Metadata["order_reference"] = orderId.ToString();

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created PaymentIntent {PaymentIntentId} for amount {Amount} {Currency}",
                paymentIntent.Id, amount, currency);

            return new PaymentIntentResult
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = amountInCents,
                Currency = currency.ToLowerInvariant()
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create PaymentIntent for amount {Amount}", amount);
            
            return new PaymentIntentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId);

            return new PaymentIntentResult
            {
                Success = paymentIntent.Status == "succeeded",
                PaymentIntentId = paymentIntent.Id,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                ErrorMessage = paymentIntent.Status != "succeeded" 
                    ? $"Payment status: {paymentIntent.Status}" 
                    : null
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to confirm PaymentIntent {PaymentIntentId}", paymentIntentId);
            
            return new PaymentIntentResult
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task CancelPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            await service.CancelAsync(paymentIntentId);
            
            _logger.LogInformation("Cancelled PaymentIntent {PaymentIntentId}", paymentIntentId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel PaymentIntent {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PaymentIntentStatusResult> GetPaymentIntentStatusAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId, new PaymentIntentGetOptions
            {
                Expand = new List<string> { "payment_method" }
            });

            var result = new PaymentIntentStatusResult
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                IsSucceeded = paymentIntent.Status == "succeeded",
                IsCanceled = paymentIntent.Status == "canceled",
                RequiresAction = paymentIntent.Status == "requires_action",
                Amount = paymentIntent.Amount / 100m, // Convert back from cents
                Currency = paymentIntent.Currency
            };

            // Extract card details if available
            if (paymentIntent.PaymentMethod?.Card != null)
            {
                result.CardLast4 = paymentIntent.PaymentMethod.Card.Last4;
                result.CardBrand = paymentIntent.PaymentMethod.Card.Brand;
            }

            if (paymentIntent.LastPaymentError != null)
            {
                result.ErrorMessage = paymentIntent.LastPaymentError.Message;
            }

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get PaymentIntent status {PaymentIntentId}", paymentIntentId);
            
            return new PaymentIntentStatusResult
            {
                PaymentIntentId = paymentIntentId,
                Status = "error",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _options.WebhookSecret);

            _logger.LogInformation("Processing Stripe webhook event {EventType}", stripeEvent.Type);

            var result = new WebhookProcessResult
            {
                Success = true,
                EventType = stripeEvent.Type
            };

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent?.Metadata?.TryGetValue("order_reference", out var orderId) == true)
                    {
                        result.OrderId = orderId;
                    }
                    _logger.LogInformation(
                        "Payment succeeded for PaymentIntent {PaymentIntentId}",
                        paymentIntent?.Id);
                    break;

                case "payment_intent.payment_failed":
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogWarning(
                        "Payment failed for PaymentIntent {PaymentIntentId}: {Error}",
                        failedIntent?.Id,
                        failedIntent?.LastPaymentError?.Message);
                    break;

                default:
                    _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to process Stripe webhook");
            
            return new WebhookProcessResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

   
}

