using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IProductTagAppService : ICrudAppService<ProductTagDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProductTagDto>
{
    Task<List<ProductTagDto>> GetByProductIdAsync(Guid productId);
}
