using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace APME.Products;

public class AttributeOptionAppService : CrudAppService<AttributeOption, AttributeOptionDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateAttributeOptionDto>, IAttributeOptionAppService
{
    public AttributeOptionAppService(IRepository<AttributeOption, Guid> repository) : base(repository)
    {
    }

    public async Task<List<AttributeOptionDto>> GetByAttributeIdAsync(Guid productAttributeId)
    {
        var options = await Repository.GetQueryableAsync();
        var query = options.Where(o => o.ProductAttributeId == productAttributeId).OrderBy(o => o.DisplayOrder);
        return ObjectMapper.Map<List<AttributeOption>, List<AttributeOptionDto>>(await AsyncExecuter.ToListAsync(query));
    }
}
