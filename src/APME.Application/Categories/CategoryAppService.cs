using System;
using System.IO;
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

    public async Task<string> UploadCategoryImageAsync(Guid categoryId, IFormFile file)
    {
        var category = await Repository.GetAsync(categoryId);
        
        // Delete old image if exists
        if (!string.IsNullOrWhiteSpace(category.ImageUrl))
        {
            await DeleteCategoryImageAsync(categoryId);
        }
        
        // Save image to blob storage
        var blobName = await _blobStorageService.SaveCategoryImageAsync(file, categoryId);
        
        // Construct image URL - ABP auto controllers will expose this at /api/app/category/{categoryId}/upload-category-image
        // Store blob name for retrieval, URL will be constructed when needed
        var baseUrl = _configuration["App:SelfUrl"]?.TrimEnd('/') ?? "";
        var imageUrl = $"{baseUrl}/api/app/category/{categoryId}/get-category-image/{blobName}";
        
        // Update category with new image URL
        category.ImageUrl = imageUrl;
        await Repository.UpdateAsync(category);
        
        return imageUrl;
    }

    public async Task DeleteCategoryImageAsync(Guid categoryId)
    {
        var category = await Repository.GetAsync(categoryId);
        
        if (string.IsNullOrWhiteSpace(category.ImageUrl))
        {
            return; // No image to delete
        }
        
        // Extract blob name from URL
        var blobName = ExtractBlobNameFromUrl(category.ImageUrl);
        
        // Delete from blob storage
        if (!string.IsNullOrWhiteSpace(blobName))
        {
            await _blobStorageService.DeleteCategoryImageAsync(blobName);
        }
        
        // Clear image URL from category
        category.ImageUrl = null;
        await Repository.UpdateAsync(category);
    }

    private string ExtractBlobNameFromUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        // Extract blob name from URL like: /api/app/category/{categoryId}/image/{blobName}
        var parts = imageUrl.Split('/');
        var imageIndex = Array.IndexOf(parts, "image");
        if (imageIndex >= 0 && imageIndex < parts.Length - 1)
        {
            return parts[imageIndex + 1];
        }

        // If URL format is different, try to extract filename
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

