using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Costing;

/// <summary>Record a purchase receipt: quantity received at a unit cost, plus any per-unit landed extras.</summary>
public class RecordReceiptInput
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid ShopId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ExtraLandedPerUnit { get; set; }

    public string? Reference { get; set; }
}
