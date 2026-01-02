using System;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.MultiTenancy;

namespace APME.BlobStorage;

/// <summary>
/// Implementation of IImageUrlProvider that converts blob names to full URLs
/// using the file system blob storage configuration
/// </summary>
public class ImageUrlProvider : IImageUrlProvider
{
    private readonly IConfiguration _configuration;
    private readonly DefaultBlobContainerConfigurationProvider _blobContainerConfigurationProvider;
    private readonly DefaultBlobFilePathCalculator _defaultBlobFilePathCalculator;
    private readonly ICurrentTenant _currentTenant;

    public ImageUrlProvider(
        IConfiguration configuration,
        DefaultBlobContainerConfigurationProvider blobContainerConfigurationProvider,
        DefaultBlobFilePathCalculator defaultBlobFilePathCalculator,
        ICurrentTenant currentTenant)
    {
        _configuration = configuration;
        _blobContainerConfigurationProvider = blobContainerConfigurationProvider;
        _defaultBlobFilePathCalculator = defaultBlobFilePathCalculator;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public string? GetFullImageUrl(string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        // If already a full URL, return as-is
        if (blobName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return blobName;
        }

        try
        {
            // Use null tenant to access host-level blob storage
            using (_currentTenant.Change(null))
            {
                var containerName = BlobContainerNameAttribute.GetContainerName<ImageContainer>();
                var blobContainerConfiguration = _blobContainerConfigurationProvider.Get(containerName);
                var physicalImagePath = _defaultBlobFilePathCalculator.Calculate(
                    new BlobProviderGetArgs(containerName, blobContainerConfiguration, blobName));
                
                int wwwrootIndex = physicalImagePath.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
                if (wwwrootIndex < 0)
                {
                    return null;
                }
                
                string relativePath = physicalImagePath.Substring(wwwrootIndex + "wwwroot".Length);
                return $"{_configuration.GetSection("App")["SelfUrl"]}{relativePath}";
            }
        }
        catch
        {
            return null;
        }
    }
}

