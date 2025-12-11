using System;
using APME.Products;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class ProductAttributeAppService : CrudAppService<ProductAttribute, ProductAttributeDto, Guid, GetProductAttributeListInput, CreateUpdateProductAttributeDto>, IProductAttributeAppService
{
    public ProductAttributeAppService(IRepository<ProductAttribute, Guid> repository) : base(repository)
    {
    }
}

