using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Products;

public interface IProductAttributeAppService : ICrudAppService<ProductAttributeDto, Guid, GetProductAttributeListInput, CreateUpdateProductAttributeDto>
{
}

