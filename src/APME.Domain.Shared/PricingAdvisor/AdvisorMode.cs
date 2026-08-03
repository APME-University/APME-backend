namespace APME.PricingAdvisor;

/// <summary>Data-sufficiency mode; gates which engine runs (rules vs the price-aware GRU).</summary>
public enum AdvisorMode
{
    ColdStart = 0,
    EarlySales = 1,
    Standard = 2,
    FullPriceResponse = 3
}
