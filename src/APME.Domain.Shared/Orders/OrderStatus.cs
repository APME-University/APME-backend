namespace APME.Orders;

/// <summary>
/// Represents the status of an order
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is pending payment confirmation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been confirmed
    /// </summary>
    PaymentConfirmed = 1,

    /// <summary>
    /// Order is being processed/prepared
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Order has been shipped
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// Order has been delivered
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// Order was cancelled
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Order was refunded
    /// </summary>
    Refunded = 6,

    /// <summary>
    /// Payment failed
    /// </summary>
    PaymentFailed = 7
}

