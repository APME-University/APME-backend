using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IAttributeOptionAppService : ICrudAppService<AttributeOptionDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateAttributeOptionDto>
{
    Task<List<AttributeOptionDto>> GetByAttributeIdAsync(Guid productAttributeId);
}
