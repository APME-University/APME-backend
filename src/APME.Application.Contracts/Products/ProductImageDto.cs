using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductImageDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public string Url { get; set; }

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}
