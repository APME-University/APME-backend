using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Categories;

public interface ICategoryAppService : ICrudAppService<CategoryDto, Guid, GetCategoryListInput, CreateUpdateCategoryDto>
{
}

