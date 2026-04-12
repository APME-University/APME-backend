using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IProductVariantAppService : ICrudAppService<ProductVariantDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProductVariantDto>
{
    Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId);
}
