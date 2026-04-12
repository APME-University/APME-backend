using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace APME.Products;

public class ProductImageAppService : CrudAppService<ProductImage, ProductImageDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateProductImageDto>, IProductImageAppService
{
    public ProductImageAppService(IRepository<ProductImage, Guid> repository) : base(repository)
    {
    }

    public async Task<List<ProductImageDto>> GetByProductIdAsync(Guid productId)
    {
        var images = await Repository.GetQueryableAsync();
        var query = images.Where(i => i.ProductId == productId).OrderBy(i => i.DisplayOrder);
        return ObjectMapper.Map<List<ProductImage>, List<ProductImageDto>>(await AsyncExecuter.ToListAsync(query));
    }
}
