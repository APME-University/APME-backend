using System;
using System.Linq;
using System.Threading.Tasks;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class ProductAttributeAppService : CrudAppService<ProductAttribute, ProductAttributeDto, Guid, GetProductAttributeListInput, CreateUpdateProductAttributeDto>, IProductAttributeAppService
{
    public ProductAttributeAppService(IRepository<ProductAttribute, Guid> repository) : base(repository)
    {
    }

    protected override async Task<IQueryable<ProductAttribute>> CreateFilteredQueryAsync(GetProductAttributeListInput input)
    {
        var queryable = await Repository.GetQueryableAsync();
        
        // Apply standard filter (handled by base class for Filter property)
        queryable = await base.CreateFilteredQueryAsync(input);
        
        // Apply custom filters following ABP.IO practices
        if (input.ShopId.HasValue)
        {
            queryable = queryable.Where(x => x.ShopId == input.ShopId.Value);
        }
        
        if (input.DataType.HasValue)
        {
            queryable = queryable.Where(x => x.DataType == input.DataType.Value);
        }
        
        return queryable;
    }
}

