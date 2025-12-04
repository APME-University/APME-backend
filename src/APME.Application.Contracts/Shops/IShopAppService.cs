using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Shops;

public interface IShopAppService : ICrudAppService<ShopDto, Guid, GetShopListInput, CreateUpdateShopDto>
{
    Task<ShopDto> ActivateAsync(Guid id);
    Task<ShopDto> DeactivateAsync(Guid id);
}

