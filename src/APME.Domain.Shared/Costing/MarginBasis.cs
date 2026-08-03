namespace APME.Costing;

/// <summary>
/// Which margin the guardrails and recommendations enforce. Contribution accounts for variable
/// selling costs (the true economics); Gross uses landed cost only.
/// </summary>
public enum MarginBasis
{
    Contribution = 0,
    Gross = 1
}
