using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace APME.Categories;

public interface ICategoryAppService : ICrudAppService<CategoryDto, Guid, GetCategoryListInput, CreateUpdateCategoryDto>
{
    // Image management methods
    Task<string> UploadCategoryImageAsync(Guid categoryId, IFormFile file);
    Task DeleteCategoryImageAsync(Guid categoryId);
    Task<IRemoteStreamContent> GetCategoryImageAsync(Guid categoryId, string blobName);
}

