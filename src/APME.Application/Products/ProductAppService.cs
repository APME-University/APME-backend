using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class ProductAppService : CrudAppService<Product, ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>, IProductAppService
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;
    private readonly DefaultBlobContainerConfigurationProvider _blobContainerConfigurationProvider;
    private readonly DefaultBlobFilePathCalculator _defaultBlobFilePathCalculator;
    private readonly IBlobContainer<ImageContainer> _container;
    private readonly IRepository<ProductAttribute, Guid> _productAttributeRepository;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        BlobStorageService blobStorageService,
        IConfiguration configuration,
        DefaultBlobContainerConfigurationProvider blobContainerConfigurationProvider,
        DefaultBlobFilePathCalculator defaultBlobFilePathCalculator,
        IBlobContainer<ImageContainer> container,
        IRepository<ProductAttribute, Guid> productAttributeRepository) : base(repository)
    {
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _blobContainerConfigurationProvider = blobContainerConfigurationProvider;
        _defaultBlobFilePathCalculator = defaultBlobFilePathCalculator;
        _container = container;
        _productAttributeRepository = productAttributeRepository;
    }

    public override async Task<ProductDto> CreateAsync([FromForm] CreateUpdateProductDto input)
    {
        // Validate and normalize attributes
        input.Attributes = await ValidateAndNormalizeAttributesAsync(input.Attributes, input.ShopId);
        
        // Map the input DTO to a Product entity
        var product = MapToEntity(input);
        
        // Save images for the product if provided (following Court pattern)
        if (input.Images is not null && input.Images.Any())
        {
            await SaveImages(input.Images, product);
        }
        else if (input.Image is not null)
        {
            // Backward compatibility: single image
            await SaveImage(product, input.Image, isPrimary: true);
        }
        
        // Insert the new product into the repository
        await Repository.InsertAsync(product, autoSave: true);
        
        // Return the created product DTO
        return MapToGetOutputDto(product);
    }

    public override async Task<ProductDto> UpdateAsync(Guid id, [FromForm] CreateUpdateProductDto input)
    {
        var product = await Repository.GetAsync(id);
        
        // Validate and normalize attributes
        input.Attributes = await ValidateAndNormalizeAttributesAsync(input.Attributes, product.ShopId);
        
        // Map input to entity
        MapToEntity(input, product);
        
        // Save new images if provided (adds to existing images, doesn't replace) - following Court pattern
        if (input.Images is not null && input.Images.Any())
        {
            await SaveImages(input.Images, product);
        }
        else if (input.Image is not null)
        {
            // Backward compatibility: single image
            await SaveImage(product, input.Image, isPrimary: false);
        }
        
        await Repository.UpdateAsync(product, autoSave: true);
        
        return MapToGetOutputDto(product);
    }

    public async Task<ProductDto> ActivateAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Activate();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> DeactivateAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Deactivate();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> PublishAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Publish();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> UnpublishAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Unpublish();
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> IncreaseStockAsync(Guid id, int quantity)
    {
        var product = await Repository.GetAsync(id);
        product.IncreaseStock(quantity);
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> DecreaseStockAsync(Guid id, int quantity)
    {
        var product = await Repository.GetAsync(id);
        product.DecreaseStock(quantity);
        await Repository.UpdateAsync(product);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<string> UploadProductImageAsync(Guid productId, IFormFile file, bool isPrimary = false)
    {
        var product = await Repository.GetAsync(productId);
        
        if (product is null)
        {
            throw new InvalidOperationException($"Product with id {productId} not found");
        }
        
        // Use CurrentTenant.Change(null) for blob operations (following Court pattern)
        using (CurrentTenant.Change(null))
        {
            // Generate unique image name following Court pattern
            var imageName = GuidGenerator.Create() + Regex.Replace(file.FileName ?? "image", @"\s+", string.Empty);
            
            // Save to blob container (overwrite: false to match Court pattern)
            using var stream = file.OpenReadStream();
            await _container.SaveAsync(imageName, stream, false);
            
            // Store blob name (not full URL) - will be converted to full URL in MapToGetOutputDto
            var blobName = imageName;
            
            // Add image to product's image list
            product.AddImage(blobName);
            
            // Set as primary if requested or if no primary exists
            if (isPrimary || string.IsNullOrWhiteSpace(product.PrimaryImageUrl))
            {
                product.SetPrimaryImage(blobName);
            }
            
            await Repository.UpdateAsync(product, autoSave: true);
            
            // Return full URL for immediate use
            return GetPhysicalPathByName(blobName);
        }
    }

    public async Task DeleteProductImageAsync(Guid productId, string imageUrl)
    {
        var product = await Repository.GetAsync(productId);
        
        if (product is null)
        {
            throw new InvalidOperationException($"Product with id {productId} not found");
        }
        
        // Extract blob name from URL (handles both full URL and blob name)
        var blobName = ExtractBlobNameFromUrl(imageUrl);
        
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new InvalidOperationException($"Could not extract blob name from URL: {imageUrl}");
        }
        
        // Remove image from product's image list
        product.RemoveImage(blobName);
        
        // Delete from blob storage (using CurrentTenant.Change(null) like Court)
        using (CurrentTenant.Change(null))
        {
            try
            {
                await _container.DeleteAsync(blobName);
            }
            catch
            {
                // Ignore if blob doesn't exist (like Court pattern)
            }
        }
        
        await Repository.UpdateAsync(product, autoSave: true);
    }

    public async Task SetPrimaryImageAsync(Guid productId, string imageUrl)
    {
        var product = await Repository.GetAsync(productId);
        
        if (product is null)
        {
            throw new InvalidOperationException($"Product with id {productId} not found");
        }
        
        // Extract blob name from URL (handles both full URL and blob name)
        var blobName = ExtractBlobNameFromUrl(imageUrl);
        
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new InvalidOperationException($"Could not extract blob name from URL: {imageUrl}");
        }
        
        // Verify image exists in product's image list (compare blob names)
        var imageList = product.GetImageList();
        if (!imageList.Contains(blobName))
        {
            throw new InvalidOperationException($"Image {blobName} does not exist for product {productId}");
        }
        
        product.SetPrimaryImage(blobName);
        await Repository.UpdateAsync(product, autoSave: true);
    }

    public async Task<List<string>> GetProductImagesAsync(Guid productId)
    {
        var product = await Repository.GetAsync(productId);
        return product.GetImageList();
    }

    public async Task<IRemoteStreamContent> GetProductImageAsync(Guid productId, string blobName)
    {
        // Use CurrentTenant.Change(null) for blob operations (following Court pattern)
        using (CurrentTenant.Change(null))
        {
            var stream = await _container.GetAsync(blobName);
            var contentType = GetContentTypeFromBlobName(blobName);
            
            return new RemoteStreamContent(stream, contentType, blobName);
        }
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

        // Extract blob name from physical path URL (following Court pattern)
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

    private async Task SaveImages(IList<IRemoteStreamContent> images, Product product)
    {
        using (CurrentTenant.Change(null))
        {
            var imageNames = await Task.WhenAll(images.Select(async image =>
            {
                var imageName = GuidGenerator.Create() + Regex.Replace(image.FileName ?? "image", @"\s+", string.Empty);
                await _container.SaveAsync(imageName, image.GetStream(), false);
                return imageName;
            }));
            
            // Add images to product's image list (following Court pattern)
            foreach (var blobName in imageNames)
            {
                product.AddImage(blobName);
            }
            
            // Set first image as primary if no primary exists
            if (imageNames.Length > 0 && string.IsNullOrWhiteSpace(product.PrimaryImageUrl))
            {
                product.SetPrimaryImage(imageNames[0]);
            }
            
            // Force garbage collection like Court does (for large images)
            GC.Collect();
        }
    }

    private async Task SaveImage(Product product, IRemoteStreamContent image, bool isPrimary = false)
    {
        using (CurrentTenant.Change(null))
        {
            // Generate unique image name following Court pattern
            var imageName = GuidGenerator.Create() + Regex.Replace(image.FileName ?? "image", @"\s+", string.Empty);
            
            // Save to blob container (overwrite: false to match Court pattern)
            await _container.SaveAsync(imageName, image.GetStream(), false);
            
            // Store blob name (not full URL) - will be converted to full URL in MapToGetOutputDto
            var blobName = imageName;
            
            // Add image to product's image list
            product.AddImage(blobName);
            
            // Set as primary if requested or if no primary exists
            if (isPrimary || string.IsNullOrWhiteSpace(product.PrimaryImageUrl))
            {
                product.SetPrimaryImage(blobName);
            }
            
            // Force garbage collection like Court does (for large images)
            GC.Collect();
        }
    }

    protected override ProductDto MapToGetOutputDto(Product entity)
    {
        var output = base.MapToGetOutputDto(entity);

        // Map images using tenant context (following Court pattern)
        using (CurrentTenant.Change(entity.TenantId))
        {
            // Map primary image URL
            if (!string.IsNullOrWhiteSpace(entity.PrimaryImageUrl))
            {
                // PrimaryImageUrl stores blob name, convert to full URL
                if (!entity.PrimaryImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    output.PrimaryImageUrl = GetPhysicalPathByName(entity.PrimaryImageUrl);
                }
                else
                {
                    output.PrimaryImageUrl = entity.PrimaryImageUrl;
                }
            }

            // Map image URLs list (entity.ImageUrls contains blob names as JSON)
            if (output.ImageUrls != null && output.ImageUrls.Count > 0)
            {
                output.ImageUrls = output.ImageUrls.Select(blobName =>
                {
                    if (!string.IsNullOrWhiteSpace(blobName) && !blobName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        return GetPhysicalPathByName(blobName);
                    }
                    return blobName;
                }).ToList();
            }
        }

        return output;
    }

    /// <summary>
    /// Validates and normalizes product attributes
    /// </summary>
    /// <param name="attributesJson">JSON string containing attribute values</param>
    /// <param name="shopId">Shop ID for loading attribute definitions</param>
    /// <returns>Normalized attributes JSON string, or null if empty</returns>
    private async Task<string?> ValidateAndNormalizeAttributesAsync(string? attributesJson, Guid shopId)
    {
        // Normalize attributes first (removes empty values, ensures consistent format)
        var normalizedAttributes = ProductAttributeNormalizer.NormalizeAttributes(attributesJson);
        
        // If attributes are provided, validate them against shop's attribute definitions
        if (!string.IsNullOrWhiteSpace(normalizedAttributes))
        {
            // Load attribute definitions for the shop
            var attributeDefinitions = await _productAttributeRepository
                .GetListAsync(x => x.ShopId == shopId);

            if (attributeDefinitions != null && attributeDefinitions.Count > 0)
            {
                var definitions = ObjectMapper.Map<List<ProductAttribute>, List<ProductAttributeDto>>(attributeDefinitions);
                
                var validationResult = ProductAttributeValidator.ValidateAttributes(normalizedAttributes, definitions);
                
                if (!validationResult.IsValid)
                {
                    throw new UserFriendlyException(
                        "Attribute validation failed: " + string.Join("; ", validationResult.Errors)
                    );
                }
            }
        }
        
        // Return normalized attributes (or null if empty)
        return normalizedAttributes;
    }
}

