using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace APME.Products;

public class ProductVariantAppService : CrudAppService<ProductVariant, ProductVariantDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateProductVariantDto>, IProductVariantAppService
{
    public ProductVariantAppService(IRepository<ProductVariant, Guid> repository) : base(repository)
    {
    }

    public async Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId)
    {
        var variants = await Repository.GetQueryableAsync();
        var query = variants.Where(v => v.ProductId == productId).OrderBy(v => v.Name);
        return ObjectMapper.Map<List<ProductVariant>, List<ProductVariantDto>>(await AsyncExecuter.ToListAsync(query));
    }
}
