namespace APME.Costing;

/// <summary>
/// How a cost component's amount is expressed.
/// PerUnit = fixed money/unit; Percentage = fraction of selling price (0.15 = 15%);
/// TotalToAllocate = a lump sum split across received units by the allocation basis.
/// </summary>
public enum CostValueType
{
    PerUnit = 0,
    Percentage = 1,
    TotalToAllocate = 2
}
