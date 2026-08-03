namespace APME.PricingAdvisor;

/// <summary>An in-memory guardrail verdict for a candidate price. Persisted as jsonb on PriceCandidate.</summary>
public record GuardrailEvaluation(GuardrailRule Rule, GuardrailStatus Status, string Message);
