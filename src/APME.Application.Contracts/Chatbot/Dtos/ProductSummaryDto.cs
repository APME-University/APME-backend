using System;

namespace APME.Chatbot.Dtos;

public class ProductSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }
    public bool InStock { get; set; }
}
