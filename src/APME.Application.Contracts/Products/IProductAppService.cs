using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace APME.Products;

public interface IProductAppService : ICrudAppService<ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>
{
    Task<ProductDto> ActivateAsync(Guid id);
    Task<ProductDto> DeactivateAsync(Guid id);
    Task<ProductDto> PublishAsync(Guid id);
    Task<ProductDto> UnpublishAsync(Guid id);
    Task<ProductDto> IncreaseStockAsync(Guid id, int quantity);
    Task<ProductDto> DecreaseStockAsync(Guid id, int quantity);
    
    // Image management methods
    Task<string> UploadProductImageAsync(Guid productId, IFormFile file, bool isPrimary = false);
    Task DeleteProductImageAsync(Guid productId, string imageUrl);
    Task SetPrimaryImageAsync(Guid productId, string imageUrl);
    Task<List<string>> GetProductImagesAsync(Guid productId);
    Task<IRemoteStreamContent> GetProductImageAsync(Guid productId, string blobName);
}

