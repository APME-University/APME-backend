namespace APME.Costing;

/// <summary>Unit economics for a product at a given price (mirrors the domain calculation).</summary>
public class UnitEconomicsDto
{
    public decimal Price { get; set; }
    public decimal LandedCost { get; set; }
    public decimal VariableSellingCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal ContributionMargin { get; set; }
    public decimal ContributionRatio { get; set; }
    public decimal Markup { get; set; }
    public decimal BreakEvenFloor { get; set; }
}
