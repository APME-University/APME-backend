using System;
using APME.Categories;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Categories;

public class CategoryAppService : CrudAppService<Category, CategoryDto, Guid, GetCategoryListInput, CreateUpdateCategoryDto>, ICategoryAppService
{
    public CategoryAppService(IRepository<Category, Guid> repository) : base(repository)
    {
    }
}

