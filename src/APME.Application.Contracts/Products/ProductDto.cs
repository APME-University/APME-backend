using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }

    public Guid? CategoryId { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string? Description { get; set; }

    public string SKU { get; set; }

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public string? Attributes { get; set; }

    public bool IsInStock => StockQuantity > 0;

    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
}

