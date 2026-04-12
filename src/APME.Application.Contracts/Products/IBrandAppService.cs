using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IBrandAppService : ICrudAppService<BrandDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateBrandDto>
{
}
