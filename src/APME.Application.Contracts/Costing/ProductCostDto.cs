using System;
using System.Collections.Generic;

namespace APME.Costing;

public class ProductCostDto
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public CostingMethod Method { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal CurrentLandedUnitCost { get; set; }
    public int QtyOnHand { get; set; }
    public DateTime? LastCostedAt { get; set; }
    public List<CostComponentDto> Components { get; set; } = new();
}
