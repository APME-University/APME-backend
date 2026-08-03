using System;
using Shouldly;
using Xunit;

namespace APME.Costing;

public class ProductCostProfileTests
{
    private static ProductCostProfile NewProfile() =>
        new ProductCostProfile(Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), "USD");

    [Fact]
    public void ApplyReceipt_updates_moving_average_and_quantity()
    {
        var p = NewProfile();

        p.ApplyReceipt(100, 10m, DateTime.UtcNow);
        p.CurrentLandedUnitCost.ShouldBe(10m);
        p.QtyOnHand.ShouldBe(100);

        p.ApplyReceipt(100, 12m, DateTime.UtcNow);
        p.CurrentLandedUnitCost.ShouldBe(11m); // (100*10 + 100*12)/200
        p.QtyOnHand.ShouldBe(200);
    }

    [Fact]
    public void SetLandedUnitCost_sets_cost_and_stamps_time()
    {
        var p = NewProfile();
        var at = DateTime.UtcNow;
        p.SetLandedUnitCost(14.20m, at);
        p.CurrentLandedUnitCost.ShouldBe(14.20m);
        p.LastCostedAt.ShouldBe(at);
        p.HasCost.ShouldBeTrue();
    }

    [Fact]
    public void ApplyReceipt_rejects_nonpositive_quantity()
    {
        var p = NewProfile();
        Should.Throw<ArgumentException>(() => p.ApplyReceipt(0, 10m, DateTime.UtcNow));
    }

    [Fact]
    public void SetLandedUnitCost_rejects_negative()
    {
        var p = NewProfile();
        Should.Throw<ArgumentException>(() => p.SetLandedUnitCost(-1m, DateTime.UtcNow));
    }
}
