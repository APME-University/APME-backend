using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IProductImageAppService : ICrudAppService<ProductImageDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProductImageDto>
{
    Task<List<ProductImageDto>> GetByProductIdAsync(Guid productId);
}
