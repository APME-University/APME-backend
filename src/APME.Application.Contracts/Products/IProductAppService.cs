using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IProductAppService : ICrudAppService<ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>
{
    Task<ProductDto> ActivateAsync(Guid id);
    Task<ProductDto> DeactivateAsync(Guid id);
    Task<ProductDto> PublishAsync(Guid id);
    Task<ProductDto> UnpublishAsync(Guid id);
    Task<ProductDto> IncreaseStockAsync(Guid id, int quantity);
    Task<ProductDto> DecreaseStockAsync(Guid id, int quantity);
}

