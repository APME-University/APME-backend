namespace APME.Costing;

public class CreateUpdateCostingPolicyDto
{
    public CostingMethod DefaultMethod { get; set; } = CostingMethod.MovingAverage;
    public MarginBasis MarginBasis { get; set; } = MarginBasis.Contribution;
    public string Currency { get; set; } = "USD";

    // Operating rates — fractions (0.15 = 15%)
    public decimal MarketplaceFeePct { get; set; }
    public decimal PaymentFeePct { get; set; }
    public decimal PaymentFixedPerUnit { get; set; }
    public decimal FulfillmentPerUnit { get; set; }
    public decimal StoragePerUnitMonth { get; set; }
    public decimal ReturnsReservePct { get; set; }
    public decimal AdPerUnit { get; set; }

    public bool IsActive { get; set; } = true;
}
