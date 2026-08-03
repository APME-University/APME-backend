namespace APME.Costing;

/// <summary>
/// How a product's landed unit cost is derived. Perpetual inventory uses MovingAverage.
/// Fifo is reserved for a later increment (layer-tracked); v1 supports the first three.
/// </summary>
public enum CostingMethod
{
    MovingAverage = 0,
    Standard = 1,
    LastPurchase = 2,
    Fifo = 3 // reserved · not implemented in v1
}
