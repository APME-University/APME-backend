using System;
using System.Collections.Generic;
using System.Linq;
using APME.Costing;
using Volo.Abp.DependencyInjection;

namespace APME.PricingAdvisor;

public interface IGuardrailEngine
{
    GuardrailOutcome Evaluate(UnitEconomicsResult econ, decimal currentPrice, PricingPolicy? policy);
}

/// <summary>
/// Enforces the guardrails using the costing engine's fee-aware break-even floor and contribution margin.
/// Any Block wins; otherwise a Cap; otherwise Pass.
/// </summary>
public class GuardrailEngine : IGuardrailEngine, ITransientDependency
{
    public GuardrailOutcome Evaluate(UnitEconomicsResult econ, decimal currentPrice, PricingPolicy? policy)
    {
        var minMargin = policy?.MinMarginPct ?? 0.20m;
        var maxDiscount = policy?.MaxDiscountPct ?? 0.15m;
        var maxIncrease = policy?.MaxIncreasePct ?? 0.15m;
        var pct = currentPrice > 0m ? (econ.Price - currentPrice) / currentPrice : 0m;

        var evals = new List<GuardrailEvaluation>
        {
            econ.Price >= econ.BreakEvenFloor
                ? new GuardrailEvaluation(GuardrailRule.CostFloor, GuardrailStatus.Pass, "Above break-even floor")
                : new GuardrailEvaluation(GuardrailRule.CostFloor, GuardrailStatus.Block,
                    $"Below fee-aware break-even floor {econ.BreakEvenFloor:0.00}"),

            econ.ContributionRatio >= minMargin
                ? new GuardrailEvaluation(GuardrailRule.MinMargin, GuardrailStatus.Pass, "Meets minimum margin")
                : new GuardrailEvaluation(GuardrailRule.MinMargin, GuardrailStatus.Block,
                    $"Contribution {econ.ContributionRatio:P0} below minimum {minMargin:P0}")
        };

        if (pct < 0m && Math.Abs(pct) > maxDiscount)
        {
            evals.Add(new GuardrailEvaluation(GuardrailRule.MaxDiscount, GuardrailStatus.Cap, $"Discount exceeds {maxDiscount:P0}"));
        }

        if (pct > maxIncrease)
        {
            evals.Add(new GuardrailEvaluation(GuardrailRule.MaxIncrease, GuardrailStatus.Cap, $"Increase exceeds {maxIncrease:P0}"));
        }

        var status = evals.Any(e => e.Status == GuardrailStatus.Block)
            ? GuardrailStatus.Block
            : evals.Any(e => e.Status == GuardrailStatus.Cap)
                ? GuardrailStatus.Cap
                : GuardrailStatus.Pass;

        return new GuardrailOutcome(status, evals);
    }
}
