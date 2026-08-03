using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Volo.Abp.DependencyInjection;

namespace APME.PricingAdvisor;

public interface ICandidateGenerator
{
    IReadOnlyList<decimal> Generate(decimal currentPrice, PricingPolicy? policy);
}

/// <summary>Builds a safe candidate-price grid from the policy grid, clamped to max discount/increase.</summary>
public class CandidateGenerator : ICandidateGenerator, ITransientDependency
{
    private static readonly List<decimal> DefaultGrid = new() { -0.10m, -0.05m, 0m, 0.05m, 0.10m };

    public IReadOnlyList<decimal> Generate(decimal currentPrice, PricingPolicy? policy)
    {
        var grid = ParseGrid(policy?.CandidateGridJson);
        var maxDiscount = policy?.MaxDiscountPct ?? 0.15m;
        var maxIncrease = policy?.MaxIncreasePct ?? 0.15m;

        var prices = new List<decimal>();
        foreach (var g in grid)
        {
            var pct = Math.Clamp(g, -maxDiscount, maxIncrease);
            var price = Math.Round(currentPrice * (1m + pct), 2);
            if (price > 0m && !prices.Contains(price))
            {
                prices.Add(price);
            }
        }

        if (!prices.Contains(currentPrice))
        {
            prices.Add(currentPrice);
        }

        return prices.OrderBy(p => p).ToList();
    }

    private static IReadOnlyList<decimal> ParseGrid(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return DefaultGrid;
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<decimal>>(json);
            return arr is { Count: > 0 } ? arr : DefaultGrid;
        }
        catch
        {
            return DefaultGrid;
        }
    }
}
