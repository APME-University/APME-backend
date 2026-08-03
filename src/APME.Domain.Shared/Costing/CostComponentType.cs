namespace APME.Costing;

/// <summary>
/// Controlled catalog of cost component types. Inventory-plane types (0-9) capitalize into the
/// landed unit cost; operating-plane types (10+) are variable selling costs. Custom/Other exists
/// on each plane as an escape hatch. The engine relies on the type to know how to treat a cost.
/// </summary>
public enum CostComponentType
{
    // Inventory plane (landed cost)
    SupplierPrice = 0,
    InboundFreight = 1,
    Duty = 2,
    Insurance = 3,
    Handling = 4,
    OtherInventory = 9,

    // Operating plane (variable selling)
    MarketplaceFee = 10,
    PaymentProcessing = 11,
    Fulfillment = 12,
    Storage = 13,
    ReturnsReserve = 14,
    AdAllocation = 15,
    OtherOperating = 19
}
