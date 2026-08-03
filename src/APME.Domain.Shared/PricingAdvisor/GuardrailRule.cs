namespace APME.PricingAdvisor;

public enum GuardrailRule
{
    CostFloor = 0,
    MinMargin = 1,
    MaxDiscount = 2,
    MaxIncrease = 3,
    StockAware = 4,
    NearExpiry = 5,
    CompetitorGap = 6,
    Confidence = 7
}
