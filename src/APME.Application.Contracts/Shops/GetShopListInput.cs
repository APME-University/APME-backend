using Volo.Abp.Application.Dtos;

namespace APME.Shops;

public class GetShopListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public bool? IsActive { get; set; }
}

