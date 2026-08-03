using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;

namespace APME.Costing;

/// <summary>
/// The costing engine consumed by the pricing advisor (ProfitCalculator / GuardrailEngine) and the
/// cost app services. Wraps <see cref="UnitEconomics"/> with product/shop data.
/// </summary>
public interface ICostingService : IDomainService
{
    Task<decimal> GetLandedUnitCostAsync(Guid productId);

    Task<UnitEconomicsResult> GetUnitEconomicsAsync(Guid productId, Guid shopId, decimal price);

    /// <summary>Record a purchase receipt: writes cost entries and moving-average updates the landed cost.</summary>
    Task ApplyPurchaseReceiptAsync(Guid productId, Guid shopId, int qty, decimal unitCost, decimal extraLandedPerUnit, string? reference = null);

    /// <summary>Recompute the landed unit cost as the sum of the product's active per-unit inventory components.</summary>
    Task<decimal> RecomputeLandedFromComponentsAsync(Guid productId, Guid shopId);

    Task<ProductCostProfile> EnsureProfileAsync(Guid productId, Guid shopId, string currency = "USD");
}
