using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace APME.PricingAdvisor;

public class RecommendationDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public PricingScenario Scenario { get; set; }
    public AdvisorMode Mode { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal RecommendedPrice { get; set; }
    public RecommendationAction Action { get; set; }
    public decimal ExpectedDemand { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal ExpectedProfit { get; set; }
    public decimal ExpectedMargin { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public RecommendationStatus Status { get; set; }
    public string? ReasonCodes { get; set; }
    public string? Explanation { get; set; }
    public Guid? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
    public List<CandidateDto> Candidates { get; set; } = new();
}

public class CandidateDto : EntityDto<Guid>
{
    public decimal Price { get; set; }
    public decimal PctChange { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal ExpectedProfit { get; set; }
    public decimal Margin { get; set; }
    public GuardrailStatus GuardrailStatus { get; set; }
    public string? GuardrailsJson { get; set; }
    public bool IsRecommended { get; set; }
}

public class GuardrailEvaluationDto
{
    public GuardrailRule Rule { get; set; }
    public GuardrailStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SimulationResultDto
{
    public decimal Price { get; set; }
    public decimal PctChange { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal ExpectedProfit { get; set; }
    public decimal Margin { get; set; }
    public GuardrailStatus GuardrailStatus { get; set; }
    public List<GuardrailEvaluationDto> Guardrails { get; set; } = new();
}

public class AttentionItemDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public decimal CurrentPrice { get; set; }
    public PricingScenario Scenario { get; set; }
    public double Priority { get; set; }
    public decimal EstimatedOpportunity { get; set; }
    public string? ReasonSummary { get; set; }
    public bool IsResolved { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class AttentionListDto
{
    public decimal TotalOpportunity { get; set; }
    public int AttentionCount { get; set; }
    public int PendingApprovals { get; set; }
    public List<AttentionItemDto> Items { get; set; } = new();
}

public class DemandHistoryPointDto
{
    public DateTime Period { get; set; }
    public decimal Units { get; set; }
    public decimal Price { get; set; }
}

public class ProductDemandHistoryDto
{
    public AdvisorMode Mode { get; set; }
    public decimal TotalUnits { get; set; }
    public decimal RecentUnits { get; set; }
    public decimal AverageUnits { get; set; }
    public int PriceChangeCount { get; set; }
    public List<DemandHistoryPointDto> Points { get; set; } = new();
}

public class ProductPricingAnalysisDto
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal LandedCost { get; set; }
    public decimal ContributionMargin { get; set; }
    public decimal BreakEvenFloor { get; set; }
    public int StockQuantity { get; set; }
    public decimal? CompetitorAvg { get; set; }
    public int CompetitorCount { get; set; }
}
