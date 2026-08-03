namespace APME.PricingAdvisor;

/// <summary>The business situation the classifier detects for a product; drives the default action.</summary>
public enum PricingScenario
{
    StableMarket = 0,
    PriceChangeOpportunity = 1,
    DemandSurge = 2,
    StaleInventory = 3,
    NearExpiry = 4,
    CompetitorUndercut = 5,
    MarginRisk = 6,
    LowData = 7,
    Custom = 8
}
