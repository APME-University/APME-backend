using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace APME.PricingAdvisor;

public class CompetitorPriceDto : FullAuditedEntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public string Competitor { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Url { get; set; }
    public DateTime CapturedAt { get; set; }
    public double MatchConfidence { get; set; }
    public bool IsAvailable { get; set; }
}

public class CreateUpdateCompetitorPriceDto
{
    [Required] public Guid ProductId { get; set; }
    [Required] public Guid ShopId { get; set; }
    [Required][StringLength(256)] public string Competitor { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Url { get; set; }
    public DateTime CapturedAt { get; set; }
    public double MatchConfidence { get; set; } = 1.0;
}

public class GetCompetitorPriceListInput : PagedAndSortedResultRequestDto
{
    public Guid? ShopId { get; set; }
    public Guid? ProductId { get; set; }
}
