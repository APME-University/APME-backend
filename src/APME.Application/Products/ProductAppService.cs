using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class ProductAppService : CrudAppService<Product, ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>, IProductAppService
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        BlobStorageService blobStorageService,
        IConfiguration configuration) : base(repository)
    {
        _blobStorageService = blobStorageService;
        _configuration = configuration;
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
        
        // Save image to blob storage
        var blobName = await _blobStorageService.SaveProductImageAsync(file, productId);
        
        // Construct image URL - ABP auto controllers will expose this at /api/app/product/{productId}/upload-product-image
        // Store blob name for retrieval, URL will be constructed when needed
        var baseUrl = _configuration["App:SelfUrl"]?.TrimEnd('/') ?? "";
        var imageUrl = $"{baseUrl}/api/app/product/{productId}/get-product-image/{blobName}";
        
        // Add image to product
        product.AddImage(imageUrl);
        
        // Set as primary if requested
        if (isPrimary || string.IsNullOrWhiteSpace(product.PrimaryImageUrl))
        {
            product.SetPrimaryImage(imageUrl);
        }
        
        await Repository.UpdateAsync(product);
        
        return imageUrl;
    }

    public async Task DeleteProductImageAsync(Guid productId, string imageUrl)
    {
        var product = await Repository.GetAsync(productId);
        
        // Extract blob name from URL
        var blobName = ExtractBlobNameFromUrl(imageUrl);
        
        // Remove image from product
        product.RemoveImage(imageUrl);
        
        // Delete from blob storage
        if (!string.IsNullOrWhiteSpace(blobName))
        {
            await _blobStorageService.DeleteProductImageAsync(blobName);
        }
        
        await Repository.UpdateAsync(product);
    }

    public async Task SetPrimaryImageAsync(Guid productId, string imageUrl)
    {
        var product = await Repository.GetAsync(productId);
        
        // Verify image exists in product's image list
        var imageList = product.GetImageList();
        if (!imageList.Contains(imageUrl))
        {
            throw new InvalidOperationException($"Image {imageUrl} does not exist for product {productId}");
        }
        
        product.SetPrimaryImage(imageUrl);
        await Repository.UpdateAsync(product);
    }

    public async Task<List<string>> GetProductImagesAsync(Guid productId)
    {
        var product = await Repository.GetAsync(productId);
        return product.GetImageList();
    }

    public async Task<IRemoteStreamContent> GetProductImageAsync(Guid productId, string blobName)
    {
        var stream = await _blobStorageService.GetProductImageAsync(blobName);
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

    private string ExtractBlobNameFromUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        // Extract blob name from URL like: /api/app/product/{productId}/images/{blobName}
        var parts = imageUrl.Split('/');
        var imagesIndex = Array.IndexOf(parts, "images");
        if (imagesIndex >= 0 && imagesIndex < parts.Length - 1)
        {
            return parts[imagesIndex + 1];
        }

        // If URL format is different, try to extract filename
        return System.IO.Path.GetFileName(imageUrl);
    }
}

