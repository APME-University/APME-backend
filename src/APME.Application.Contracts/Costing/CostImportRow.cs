using System;

namespace APME.Costing;

/// <summary>One row of a cost CSV import: a product's landed unit cost, matched by SKU within a shop.</summary>
public class CostImportRow
{
    public string Sku { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = "USD";
}
