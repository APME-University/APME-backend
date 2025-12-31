namespace APME.Carts;

/// <summary>
/// Represents the status of a shopping cart
/// </summary>
public enum CartStatus
{
    /// <summary>
    /// Cart is active and can be modified
    /// </summary>
    Active = 0,

    /// <summary>
    /// Cart has been converted to an order
    /// </summary>
    CheckedOut = 1,

    /// <summary>
    /// Cart was abandoned (for analytics/cleanup)
    /// </summary>
    Abandoned = 2
}

