namespace APME.Orders;

/// <summary>
/// Represents the payment method used for an order
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Credit or debit card via Stripe
    /// </summary>
    Card = 0,

    /// <summary>
    /// PayPal (future)
    /// </summary>
    PayPal = 1,

    /// <summary>
    /// Bank transfer (future)
    /// </summary>
    BankTransfer = 2,

    /// <summary>
    /// Cash on delivery (future)
    /// </summary>
    CashOnDelivery = 3
}

