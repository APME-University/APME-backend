using System;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class BrandAppService : CrudAppService<Brand, BrandDto, Guid, Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, CreateUpdateBrandDto>, IBrandAppService
{
    public BrandAppService(IRepository<Brand, Guid> repository) : base(repository)
    {
    }
}
