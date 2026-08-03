namespace APME.Costing;

/// <summary>
/// Static metadata for the cost component catalog: which plane a type belongs to and its natural
/// value type. The UI/services use these defaults so entry is fast and the math stays correct.
/// </summary>
public static class CostComponentTypeExtensions
{
    public static CostPlane GetPlane(this CostComponentType type)
        => (int)type >= 10 ? CostPlane.Operating : CostPlane.Inventory;

    public static CostValueType GetDefaultValueType(this CostComponentType type)
        => type switch
        {
            CostComponentType.MarketplaceFee => CostValueType.Percentage,
            CostComponentType.PaymentProcessing => CostValueType.Percentage,
            CostComponentType.ReturnsReserve => CostValueType.Percentage,
            CostComponentType.AdAllocation => CostValueType.Percentage,
            _ => CostValueType.PerUnit
        };
}
