using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductAttributeDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }

    public string Name { get; set; }

    public string DisplayName { get; set; }

    public ProductAttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }
}

