using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class GetProductAttributeListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? ShopId { get; set; }

    public ProductAttributeDataType? DataType { get; set; }
}

