using System;
using Volo.Abp.Application.Dtos;

namespace APME.Costing;

public class CostingPolicyDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }
    public CostingMethod DefaultMethod { get; set; }
    public MarginBasis MarginBasis { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal MarketplaceFeePct { get; set; }
    public decimal PaymentFeePct { get; set; }
    public decimal PaymentFixedPerUnit { get; set; }
    public decimal FulfillmentPerUnit { get; set; }
    public decimal StoragePerUnitMonth { get; set; }
    public decimal ReturnsReservePct { get; set; }
    public decimal AdPerUnit { get; set; }
    public bool IsActive { get; set; }
}
