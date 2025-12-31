using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace APME.Orders;

/// <summary>
/// Value Object representing a snapshot of payment information at the time of order
/// Immutable after creation - provides a historical record of the payment
/// </summary>
public class PaymentSnapshot : ValueObject
{
    /// <summary>
    /// Payment method used
    /// </summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>
    /// Payment gateway transaction ID (e.g., Stripe PaymentIntent ID)
    /// </summary>
    public string TransactionId { get; private set; }

    /// <summary>
    /// Amount that was charged
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Current status of the payment
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// When the payment was processed
    /// </summary>
    public DateTime ProcessedAt { get; private set; }

    /// <summary>
    /// Last 4 digits of card (if card payment)
    /// </summary>
    public string? CardLast4 { get; private set; }

    /// <summary>
    /// Card brand (visa, mastercard, etc.)
    /// </summary>
    public string? CardBrand { get; private set; }

    /// <summary>
    /// Any failure message from the payment gateway
    /// </summary>
    public string? FailureMessage { get; private set; }

    protected PaymentSnapshot()
    {
        // Required by EF Core
    }

    public PaymentSnapshot(
        PaymentMethod method,
        string transactionId,
        decimal amount,
        string currency,
        PaymentStatus status,
        DateTime processedAt,
        string? cardLast4 = null,
        string? cardBrand = null,
        string? failureMessage = null)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID is required", nameof(transactionId));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Method = method;
        TransactionId = transactionId.Trim();
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        Status = status;
        ProcessedAt = processedAt;
        CardLast4 = cardLast4?.Trim();
        CardBrand = cardBrand?.Trim();
        FailureMessage = failureMessage?.Trim();
    }

    /// <summary>
    /// Creates a payment snapshot for a successful card payment
    /// </summary>
    public static PaymentSnapshot CreateSuccessful(
        string transactionId,
        decimal amount,
        string currency,
        string? cardLast4 = null,
        string? cardBrand = null)
    {
        return new PaymentSnapshot(
            PaymentMethod.Card,
            transactionId,
            amount,
            currency,
            PaymentStatus.Captured,
            DateTime.UtcNow,
            cardLast4,
            cardBrand);
    }

    /// <summary>
    /// Creates a payment snapshot for a failed payment
    /// </summary>
    public static PaymentSnapshot CreateFailed(
        string transactionId,
        decimal amount,
        string currency,
        string failureMessage)
    {
        return new PaymentSnapshot(
            PaymentMethod.Card,
            transactionId,
            amount,
            currency,
            PaymentStatus.Failed,
            DateTime.UtcNow,
            failureMessage: failureMessage);
    }

    /// <summary>
    /// Creates a payment snapshot for a pending payment
    /// </summary>
    public static PaymentSnapshot CreatePending(
        string transactionId,
        decimal amount,
        string currency)
    {
        return new PaymentSnapshot(
            PaymentMethod.Card,
            transactionId,
            amount,
            currency,
            PaymentStatus.Pending,
            DateTime.UtcNow);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Method;
        yield return TransactionId;
        yield return Amount;
        yield return Currency;
        yield return Status;
        yield return ProcessedAt;
        yield return CardLast4 ?? string.Empty;
        yield return CardBrand ?? string.Empty;
        yield return FailureMessage ?? string.Empty;
    }
}

