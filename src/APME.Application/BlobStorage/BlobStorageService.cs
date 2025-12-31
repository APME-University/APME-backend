using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Services;

namespace APME.BlobStorage;

public class BlobStorageService : DomainService
{
    private readonly IBlobContainer<ImageContainer> _imageContainer;

    public BlobStorageService(IBlobContainer<ImageContainer> imageContainer)
    {
        _imageContainer = imageContainer;
    }

    public async Task<string> SaveProductImageAsync(IFormFile file, Guid productId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null", nameof(file));
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
        {
            throw new ArgumentException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
        }

        // Validate file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB");
        }

        // Generate unique filename: product_{productId}_{timestamp}_{originalname}
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace(" ", "_")
            .Replace("(", "")
            .Replace(")", "");
        var uniqueFileName = $"product_{productId}_{timestamp}_{sanitizedFileName}{fileExtension}";

        // Save to blob storage
        using var stream = file.OpenReadStream();
        await _imageContainer.SaveAsync(uniqueFileName, stream, true);

        // Return the blob name (URL will be constructed by the API)
        return uniqueFileName;
    }

    public async Task<string> SaveCategoryImageAsync(IFormFile file, Guid categoryId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null", nameof(file));
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
        {
            throw new ArgumentException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
        }

        // Validate file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB");
        }

        // Generate unique filename: category_{categoryId}_{timestamp}_{originalname}
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace(" ", "_")
            .Replace("(", "")
            .Replace(")", "");
        var uniqueFileName = $"category_{categoryId}_{timestamp}_{sanitizedFileName}{fileExtension}";

        // Save to blob storage
        using var stream = file.OpenReadStream();
        await _imageContainer.SaveAsync(uniqueFileName, stream, true);

        // Return the blob name (URL will be constructed by the API)
        return uniqueFileName;
    }

    public async Task<Stream> GetImageAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        return await _imageContainer.GetAsync(blobName);
    }

    public async Task DeleteImageAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return; // Silently ignore if blob name is empty
        }

        try
        {
            await _imageContainer.DeleteAsync(blobName);
        }
        catch
        {
            // Ignore if blob doesn't exist
        }
    }

    public async Task<bool> ImageExistsAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return false;
        }

        return await _imageContainer.ExistsAsync(blobName);
    }

    // Convenience methods for backward compatibility
    public async Task<Stream> GetProductImageAsync(string blobName) => await GetImageAsync(blobName);
    public async Task<Stream> GetCategoryImageAsync(string blobName) => await GetImageAsync(blobName);
    public async Task DeleteProductImageAsync(string blobName) => await DeleteImageAsync(blobName);
    public async Task DeleteCategoryImageAsync(string blobName) => await DeleteImageAsync(blobName);
    public async Task<bool> ProductImageExistsAsync(string blobName) => await ImageExistsAsync(blobName);
    public async Task<bool> CategoryImageExistsAsync(string blobName) => await ImageExistsAsync(blobName);
}

