using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductVariantDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }

    public string SKU { get; set; }

    public string Name { get; set; }

    public decimal? Price { get; set; }

    public decimal? SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public StockStatus StockStatus { get; set; } = StockStatus.InStock;

    public bool IsDefault { get; set; }

    public string? VariantAttributesJson { get; set; }

    public string StockStatusLabel => StockStatus.ToString();
}
