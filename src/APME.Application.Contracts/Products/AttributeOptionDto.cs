using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class AttributeOptionDto : EntityDto<Guid>
{
    public Guid ProductAttributeId { get; set; }

    public string Value { get; set; }

    public string DisplayValue { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation display
    public string? AttributeName { get; set; }
}
