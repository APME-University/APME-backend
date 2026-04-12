using System;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class AttributeGroupAppService : CrudAppService<AttributeGroup, AttributeGroupDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateAttributeGroupDto>, IAttributeGroupAppService
{
    public AttributeGroupAppService(IRepository<AttributeGroup, Guid> repository) : base(repository)
    {
    }
}
