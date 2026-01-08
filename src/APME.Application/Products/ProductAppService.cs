using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Events;
using APME.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Content;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
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
    private readonly ICanonicalDocumentBuilder _canonicalDocumentBuilder;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<ProductAppService> _logger;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        BlobStorageService blobStorageService,
        IConfiguration configuration,
        DefaultBlobContainerConfigurationProvider blobContainerConfigurationProvider,
        DefaultBlobFilePathCalculator defaultBlobFilePathCalculator,
        IBlobContainer<ImageContainer> container,
        IRepository<ProductAttribute, Guid> productAttributeRepository,
        ICanonicalDocumentBuilder canonicalDocumentBuilder,
        IDistributedEventBus distributedEventBus,
        ILogger<ProductAppService> logger) : base(repository)
    {
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _blobContainerConfigurationProvider = blobContainerConfigurationProvider;
        _defaultBlobFilePathCalculator = defaultBlobFilePathCalculator;
        _container = container;
        _productAttributeRepository = productAttributeRepository;
        _canonicalDocumentBuilder = canonicalDocumentBuilder;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
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
        
        // Build canonical document for AI embeddings (RAG Architecture)
        await BuildAndSaveCanonicalDocumentAsync(product);
        
        // Emit product created event for embedding generation via outbox
        await EmitProductChangedEventAsync(product, ProductChangeType.Created);
        
        _logger.LogInformation(
            "Product created: {ProductId} ({ProductName}) - Canonical document built, embedding event emitted",
            product.Id, product.Name);
        
        // Return the created product DTO
        return MapToGetOutputDto(product);
    }

    public override async Task<ProductDto> UpdateAsync(Guid id, [FromForm] CreateUpdateProductDto input)
    {
        // Note: Image operations (add, remove, set primary) are handled by separate APIs
        // to avoid concurrency issues. This method only updates product properties.
        
        var product = await Repository.GetAsync(id);
        
        // Validate and normalize attributes
        input.Attributes = await ValidateAndNormalizeAttributesAsync(input.Attributes, product.ShopId);
        
        // Map input to entity (ImageUrls and PrimaryImageUrl are ignored in AutoMapper)
        MapToEntity(input, product);
        
        // Update product without touching images
        await Repository.UpdateAsync(product, autoSave: true);
        
        // Rebuild canonical document for AI embeddings (RAG Architecture)
        await BuildAndSaveCanonicalDocumentAsync(product);
        
        // Emit product updated event for embedding regeneration via outbox
        await EmitProductChangedEventAsync(product, ProductChangeType.Updated);
        
        _logger.LogInformation(
            "Product updated: {ProductId} ({ProductName}) - Canonical document rebuilt, embedding event emitted",
            product.Id, product.Name);
        
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
        
        // Emit publish event - product now eligible for embedding/search
        await EmitProductChangedEventAsync(product, ProductChangeType.Published);
        
        _logger.LogInformation(
            "Product published: {ProductId} ({ProductName}) - Now eligible for AI search",
            product.Id, product.Name);
        
        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> UnpublishAsync(Guid id)
    {
        var product = await Repository.GetAsync(id);
        product.Unpublish();
        await Repository.UpdateAsync(product);
        
        // Emit unpublish event - product removed from search
        await EmitProductChangedEventAsync(product, ProductChangeType.Unpublished);
        
        _logger.LogInformation(
            "Product unpublished: {ProductId} ({ProductName}) - Removed from AI search",
            product.Id, product.Name);
        
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
        // Use case-insensitive comparison and handle URL-encoded names
        var imageList = product.GetImageList();
        var matchingImage = imageList.FirstOrDefault(img => 
            string.Equals(img, blobName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ExtractBlobNameFromUrl(img), blobName, StringComparison.OrdinalIgnoreCase)
        );
        
        if (string.IsNullOrWhiteSpace(matchingImage))
        {
            // Try to find by comparing just the filename part
            var blobFileName = System.IO.Path.GetFileName(blobName);
            matchingImage = imageList.FirstOrDefault(img => 
                string.Equals(System.IO.Path.GetFileName(img), blobFileName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(System.IO.Path.GetFileName(ExtractBlobNameFromUrl(img)), blobFileName, StringComparison.OrdinalIgnoreCase)
            );
        }
        
        if (string.IsNullOrWhiteSpace(matchingImage))
        {
            throw new InvalidOperationException(
                $"Image '{blobName}' does not exist for product {productId}. " +
                $"Available images: {string.Join(", ", imageList)}"
            );
        }
        
        // Use the actual blob name from the product's image list
        var actualBlobName = ExtractBlobNameFromUrl(matchingImage) ?? matchingImage;
        product.SetPrimaryImage(actualBlobName);
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

        // URL decode the input first (handles %5C, %2F, etc.)
        string decodedUrl = Uri.UnescapeDataString(imageUrl);

        // If it's already a blob name (no http/https), return as-is
        if (!decodedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return decodedUrl;
        }

        // Extract blob name from physical path URL (following Court pattern)
        // URL format: http://domain/wwwroot/uploads/images/{blobName}
        // Also handle: http://domain/uploads/images/{blobName} or http://domain\uploads\images\{blobName}
        var wwwrootIndex = decodedUrl.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
        string pathToProcess = wwwrootIndex >= 0 
            ? decodedUrl.Substring(wwwrootIndex + "wwwroot".Length)
            : decodedUrl;

        // Split by both forward and back slashes (handles URL-encoded backslashes)
        var parts = pathToProcess.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Find the last "images" segment and get the blob name after it
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (string.Equals(parts[i], "images", StringComparison.OrdinalIgnoreCase) && i < parts.Length - 1)
            {
                // Found "images" segment, return the next part (blob name)
                return parts[i + 1];
            }
        }

        // Fallback: get the last part of the path (should be the blob name)
        var lastPart = parts.LastOrDefault();
        if (!string.IsNullOrWhiteSpace(lastPart))
        {
            // Remove query parameters if present
            var queryIndex = lastPart.IndexOf('?');
            if (queryIndex >= 0)
            {
                lastPart = lastPart.Substring(0, queryIndex);
            }
            return lastPart;
        }

        // Last resort: extract filename from the full URL
        return System.IO.Path.GetFileName(decodedUrl);
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
            // Note: This modifies ImageUrls property, which EF Core tracks for concurrency
            foreach (var blobName in imageNames)
            {
                product.AddImage(blobName);
            }
            
            // Set first image as primary if no primary exists
            // Note: This modifies PrimaryImageUrl property, which EF Core tracks for concurrency
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
            // Note: This modifies ImageUrls property, which EF Core tracks for concurrency
            product.AddImage(blobName);
            
            // Set as primary if requested or if no primary exists
            // Note: This modifies PrimaryImageUrl property, which EF Core tracks for concurrency
            if (isPrimary || string.IsNullOrWhiteSpace(product.PrimaryImageUrl))
            {
                product.SetPrimaryImage(blobName);
            }
            
            // Force garbage collection like Court does (for large images)
            GC.Collect();
        }
    }

    protected override async Task<IQueryable<Product>> CreateFilteredQueryAsync(GetProductListInput input)
    {
        var queryable = await Repository.GetQueryableAsync();
        
        // Apply standard filter (handled by base class for Filter property)
        queryable = await base.CreateFilteredQueryAsync(input);
        
        // Apply custom filters following ABP.IO practices
        if (input.ShopId.HasValue)
        {
            queryable = queryable.Where(x => x.ShopId == input.ShopId.Value);
        }
        
        if (input.CategoryId.HasValue)
        {
            queryable = queryable.Where(x => x.CategoryId == input.CategoryId.Value);
        }
        
        if (input.IsActive.HasValue)
        {
            queryable = queryable.Where(x => x.IsActive == input.IsActive.Value);
        }
        
        if (input.IsPublished.HasValue)
        {
            queryable = queryable.Where(x => x.IsPublished == input.IsPublished.Value);
        }
        
        if (input.InStock.HasValue && input.InStock.Value)
        {
            queryable = queryable.Where(x => x.StockQuantity > 0);
        }
        
        return queryable;
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

    #region AI/Embedding Support (RAG Architecture)

    /// <summary>
    /// Builds and saves the canonical document for AI embedding generation.
    /// SRS Reference: AI Chatbot RAG Architecture - Canonical Document Generation
    /// </summary>
    private async Task BuildAndSaveCanonicalDocumentAsync(Product product)
    {
        try
        {
            var canonicalDocument = await _canonicalDocumentBuilder.BuildAsync(product);
            product.UpdateCanonicalDocument(canonicalDocument);
            await Repository.UpdateAsync(product, autoSave: true);
            
            _logger.LogDebug(
                "Canonical document built for product {ProductId}, version {Version}",
                product.Id, canonicalDocument.SchemaVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to build canonical document for product {ProductId}",
                product.Id);
            // Don't throw - embedding generation is non-critical for CRUD operations
        }
    }

    /// <summary>
    /// Emits a product changed event via the distributed event bus (outbox pattern).
    /// This triggers background embedding generation.
    /// SRS Reference: AI Chatbot RAG Architecture - Transactional Outbox
    /// </summary>
    private async Task EmitProductChangedEventAsync(Product product, ProductChangeType changeType)
    {
        try
        {
            var eventData = new ProductChangedEto
            {
                ProductId = product.Id,
                ShopId = product.ShopId,
                TenantId = product.TenantId,
                ProductName = product.Name,
                ChangeType = changeType,
                CanonicalDocumentVersion = product.CanonicalDocumentVersion,
                ChangedAt = DateTime.UtcNow,
                IsEligibleForEmbedding = product.IsActive && product.IsPublished
            };

            await _distributedEventBus.PublishAsync(eventData);
            
            _logger.LogDebug(
                "Product changed event emitted: {ProductId}, Type: {ChangeType}, Eligible: {IsEligible}",
                product.Id, changeType, eventData.IsEligibleForEmbedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to emit product changed event for {ProductId}",
                product.Id);
            // Don't throw - event emission failure is non-critical for CRUD operations
        }
    }

    #endregion
}

