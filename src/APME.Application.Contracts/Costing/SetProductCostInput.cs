using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APME.Costing;

/// <summary>
/// Set a product's inventory cost. Use <see cref="SimpleUnitCost"/> for the simple single-number mode,
/// or <see cref="Components"/> for the advanced per-unit breakdown (freight, duty, handling, …).
/// </summary>
public class SetProductCostInput
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid ShopId { get; set; }

    public string Currency { get; set; } = "USD";

    public CostingMethod Method { get; set; } = CostingMethod.MovingAverage;

    public decimal? SimpleUnitCost { get; set; }

    public List<CostComponentInput>? Components { get; set; }
}

public class CostComponentInput
{
    public CostComponentType ComponentType { get; set; }
    public CostValueType ValueType { get; set; } = CostValueType.PerUnit;
    public decimal Amount { get; set; }
}
