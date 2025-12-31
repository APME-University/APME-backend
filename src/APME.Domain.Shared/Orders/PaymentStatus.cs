namespace APME.Orders;

/// <summary>
/// Represents the status of a payment
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been authorized but not captured
    /// </summary>
    Authorized = 1,

    /// <summary>
    /// Payment has been captured/completed
    /// </summary>
    Captured = 2,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment was refunded
    /// </summary>
    Refunded = 4,

    /// <summary>
    /// Payment was partially refunded
    /// </summary>
    PartiallyRefunded = 5,

    /// <summary>
    /// Payment was cancelled
    /// </summary>
    Cancelled = 6
}

