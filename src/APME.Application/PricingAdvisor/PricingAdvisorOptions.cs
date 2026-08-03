namespace APME.PricingAdvisor;

/// <summary>
/// Selects and configures the demand oracle behind <see cref="IPricingAdvisorClient"/>.
/// UseStub=true keeps the rule-based <see cref="StubDemandClient"/> (default, no external
/// dependency); UseStub=false wires the FastAPI GRU service at <see cref="BaseUrl"/>.
/// </summary>
public class PricingAdvisorOptions
{
    public const string SectionName = "PricingAdvisor";

    public bool UseStub { get; set; } = true;

    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>HTTP timeout (seconds) for a forecast call.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
