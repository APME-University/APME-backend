using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace APME.PricingAdvisor;

public class GenerateRecommendationInput
{
    [Required] public Guid ProductId { get; set; }
    [Required] public Guid ShopId { get; set; }
}

public class SimulatePriceInput
{
    [Required] public Guid ProductId { get; set; }
    [Required] public Guid ShopId { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Price { get; set; }
}

public class ApproveInput
{
    public string? Note { get; set; }
}

public class GetRecommendationListInput : PagedAndSortedResultRequestDto
{
    public Guid? ShopId { get; set; }
    public Guid? ProductId { get; set; }
    public RecommendationStatus? Status { get; set; }
}
