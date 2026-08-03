namespace APME.Costing;

/// <summary>Where a cost entry originated.</summary>
public enum CostSource
{
    Manual = 0,
    Import = 1,
    PurchaseReceipt = 2,
    Policy = 3
}
