using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class GetProductListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? ShopId { get; set; }

    public Guid? CategoryId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsPublished { get; set; }

    public bool? InStock { get; set; }
}

