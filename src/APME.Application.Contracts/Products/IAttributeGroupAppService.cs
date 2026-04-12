using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IAttributeGroupAppService : ICrudAppService<AttributeGroupDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateAttributeGroupDto>
{
}
