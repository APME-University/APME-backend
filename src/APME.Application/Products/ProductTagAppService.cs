using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace APME.Products;

public class ProductTagAppService : CrudAppService<ProductTag, ProductTagDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateProductTagDto>, IProductTagAppService
{
    public ProductTagAppService(IRepository<ProductTag, Guid> repository) : base(repository)
    {
    }

    public async Task<List<ProductTagDto>> GetByProductIdAsync(Guid productId)
    {
        var tags = await Repository.GetQueryableAsync();
        var query = tags.Where(t => t.ProductId == productId).OrderBy(t => t.Tag);
        return ObjectMapper.Map<List<ProductTag>, List<ProductTagDto>>(await AsyncExecuter.ToListAsync(query));
    }
}
