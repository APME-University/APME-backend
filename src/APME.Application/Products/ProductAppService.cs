using System;
using System.Threading.Tasks;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class ProductAppService : CrudAppService<Product, ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>, IProductAppService
{
    public ProductAppService(IRepository<Product, Guid> repository) : base(repository)
    {
    }

    public async Task<ProductDto> ActivateAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Activate();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> DeactivateAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Deactivate();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> PublishAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Publish();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> UnpublishAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Unpublish();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> IncreaseStockAsync(Guid id, int quantity)
    {
        var product = await Repository.GetAsync(id);
        product.IncreaseStock(quantity);
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> DecreaseStockAsync(Guid id, int quantity)
    {
        var product = await Repository.GetAsync(id);
        product.DecreaseStock(quantity);
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
}

