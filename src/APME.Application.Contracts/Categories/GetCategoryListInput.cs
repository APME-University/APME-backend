using System;
using Volo.Abp.Application.Dtos;

namespace APME.Categories;

public class GetCategoryListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? ShopId { get; set; }

    public Guid? ParentId { get; set; }

    public bool? IsActive { get; set; }
}

