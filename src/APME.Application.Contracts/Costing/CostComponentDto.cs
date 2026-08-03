using System;
using Volo.Abp.Application.Dtos;

namespace APME.Costing;

public class CostComponentDto : FullAuditedEntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public CostComponentType ComponentType { get; set; }
    public CostPlane Plane { get; set; }
    public CostValueType ValueType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public CostSource Source { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
}
