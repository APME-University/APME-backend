using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Categories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Categories;

public class CategoryAppService : CrudAppService<Category, CategoryDto, Guid, GetCategoryListInput, CreateUpdateCategoryDto>, ICategoryAppService
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;
    private readonly DefaultBlobContainerConfigurationProvider _blobContainerConfigurationProvider;
    private readonly DefaultBlobFilePathCalculator _defaultBlobFilePathCalculator;
    private readonly IBlobContainer<ImageContainer> _container;

    public CategoryAppService(
        IRepository<Category, Guid> repository,
        BlobStorageService blobStorageService,
        IConfiguration configuration,
        DefaultBlobContainerConfigurationProvider blobContainerConfigurationProvider,
        DefaultBlobFilePathCalculator defaultBlobFilePathCalculator,
        IBlobContainer<ImageContainer> container) : base(repository)
    {
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _blobContainerConfigurationProvider = blobContainerConfigurationProvider;
        _defaultBlobFilePathCalculator = defaultBlobFilePathCalculator;
        _container = container;
    }

    public override async Task<CategoryDto> CreateAsync([FromForm] CreateUpdateCategoryDto input)
    {
        var category = MapToEntity(input);
        
        if (input.Image is not null)
        {
            await SaveImage(category, input.Image);
        }
        
        await Repository.InsertAsync(category, autoSave: true);
        
        return MapToGetOutputDto(category);
    }

    public override async Task<CategoryDto> UpdateAsync(Guid id, [FromForm] CreateUpdateCategoryDto input)
    {
        var category = await Repository.GetAsync(id);
        
        // Map input to entity
        MapToEntity(input, category);
        
        // Handle image update: delete old image if new one is provided
        if (input.Image is not null)
        {
            // Delete old image if exists
            if (!string.IsNullOrWhiteSpace(category.ImageUrl))
            {
                var oldBlobName = ExtractBlobNameFromUrl(category.ImageUrl);
                if (!string.IsNullOrWhiteSpace(oldBlobName))
                {
                    using (CurrentTenant.Change(null))
                    {
                        try
                        {
                            await _container.DeleteAsync(oldBlobName);
                        }
                        catch
                        {
                            // Ignore if blob doesn't exist
                        }
                    }
                }
            }
            
            // Save new image
            await SaveImage(category, input.Image);
        }
        
        await Repository.UpdateAsync(category, autoSave: true);
        
        return MapToGetOutputDto(category);
    }

    public async Task<string> UploadCategoryImageAsync(Guid categoryId, IFormFile file)
    {
        var category = await Repository.GetAsync(categoryId);
        
        // Delete old image if exists
        if (!string.IsNullOrWhiteSpace(category.ImageUrl))
        {
            var oldBlobName = ExtractBlobNameFromUrl(category.ImageUrl);
            if (!string.IsNullOrWhiteSpace(oldBlobName))
            {
                using (CurrentTenant.Change(null))
                {
                    try
                    {
                        await _container.DeleteAsync(oldBlobName);
                    }
                    catch
                    {
                        // Ignore if blob doesn't exist
                    }
                }
            }
        }
        
        // Save new image to blob storage (store blob name, not full URL)
        using (CurrentTenant.Change(null))
        {
            string imageName = GuidGenerator.Create() + Regex.Replace(file.FileName ?? "image", @"\s+", string.Empty);
            using var stream = file.OpenReadStream();
            await _container.SaveAsync(imageName, stream, false);
        
            // Store blob name (not full URL) - will be converted to full URL in MapToGetOutputDto
            category.ImageUrl = imageName;
            await Repository.UpdateAsync(category, autoSave: true);
            
            // Return full URL for immediate use
            return GetPhysicalPathByName(imageName);
        }
    }

    public async Task DeleteCategoryImageAsync(Guid categoryId)
    {
        var category = await Repository.GetAsync(categoryId);
        
        if (string.IsNullOrWhiteSpace(category.ImageUrl))
        {
            return; // No image to delete
        }
        
        // Extract blob name from URL or blob name
        var blobName = ExtractBlobNameFromUrl(category.ImageUrl);
        
        // Delete from blob storage
        if (!string.IsNullOrWhiteSpace(blobName))
        {
            using (CurrentTenant.Change(null))
            {
                try
                {
                    await _container.DeleteAsync(blobName);
                }
                catch
                {
                    // Ignore if blob doesn't exist
                }
            }
        }
        
        // Clear image URL from category
        category.ImageUrl = null;
        await Repository.UpdateAsync(category, autoSave: true);
    }

    private string ExtractBlobNameFromUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        // If it's already a blob name (no http/https), return as-is
        if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        // Extract blob name from physical path URL (following ProductAppService pattern)
        // URL format: http://domain/wwwroot/uploads/images/{blobName}
        var wwwrootIndex = imageUrl.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
        if (wwwrootIndex >= 0)
        {
            var pathAfterWwwroot = imageUrl.Substring(wwwrootIndex + "wwwroot".Length);
            // Remove leading slashes and get the blob name (last part after /images/)
            var parts = pathAfterWwwroot.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var imagesIndex = Array.IndexOf(parts, "images");
            if (imagesIndex >= 0 && imagesIndex < parts.Length - 1)
        {
                return parts[imagesIndex + 1];
            }
        }

        // Fallback: try to extract from URL path segments
        var urlParts = imageUrl.Split('/');
        var lastPart = urlParts.LastOrDefault();
        if (!string.IsNullOrWhiteSpace(lastPart))
        {
            return lastPart;
        }

        // Last resort: extract filename
        return System.IO.Path.GetFileName(imageUrl);
    }

    public async Task<IRemoteStreamContent> GetCategoryImageAsync(Guid categoryId, string blobName)
    {
        var stream = await _blobStorageService.GetCategoryImageAsync(blobName);
        var contentType = GetContentTypeFromBlobName(blobName);
        
        return new RemoteStreamContent(stream, contentType, blobName);
    }

    private string GetContentTypeFromBlobName(string blobName)
    {
        var extension = Path.GetExtension(blobName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    private string GetPhysicalPathByName(string name)
    {
        using (CurrentTenant.Change(null))
        {
            var containerName = BlobContainerNameAttribute.GetContainerName<ImageContainer>();
            var blobContainerConfiguration = _blobContainerConfigurationProvider.Get(containerName);
            var physicalImagePath = _defaultBlobFilePathCalculator.Calculate(new BlobProviderGetArgs(containerName, blobContainerConfiguration, name));
            int wwwrootIndex = physicalImagePath.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
            string relativePath = physicalImagePath.Substring(wwwrootIndex + "wwwroot".Length);
            return $"{_configuration.GetSection("App")["SelfUrl"]}{relativePath}";
        }
    }

    private async Task SaveImage(Category category, IRemoteStreamContent image)
    {
        using (CurrentTenant.Change(null))
        {
            string imageName = GuidGenerator.Create() + Regex.Replace(image.FileName ?? "image", @"\s+", string.Empty);
            await _container.SaveAsync(imageName, image.GetStream());
            category.ImageUrl = imageName;
        }
    }

    protected override async Task<IQueryable<Category>> CreateFilteredQueryAsync(GetCategoryListInput input)
    {
        var queryable = await Repository.GetQueryableAsync();
        
        // Apply standard filter (handled by base class for Filter property)
        queryable = await base.CreateFilteredQueryAsync(input);
        
        // Apply custom filters following ABP.IO practices
        if (input.ShopId.HasValue)
        {
            queryable = queryable.Where(x => x.ShopId == input.ShopId.Value);
        }
        
        if (input.ParentId.HasValue)
        {
            queryable = queryable.Where(x => x.ParentId == input.ParentId.Value);
        }
        
        if (input.IsActive.HasValue)
        {
            queryable = queryable.Where(x => x.IsActive == input.IsActive.Value);
        }
        
        return queryable;
    }

    protected override CategoryDto MapToGetOutputDto(Category entity)
    {
        var output = base.MapToGetOutputDto(entity);

        if (!string.IsNullOrWhiteSpace(entity.ImageUrl))
        {
            // Check if ImageUrl is already a full URL (starts with http) or just a blob name
            if (!entity.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                output.ImageUrl = GetPhysicalPathByName(entity.ImageUrl);
            }
            else
            {
                output.ImageUrl = entity.ImageUrl;
            }
        }
        return output;
    }
}

