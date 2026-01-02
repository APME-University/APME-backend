using Volo.Abp.DependencyInjection;

namespace APME.BlobStorage;

/// <summary>
/// Service for converting blob storage names to full URLs
/// </summary>
public interface IImageUrlProvider : ITransientDependency
{
    /// <summary>
    /// Converts a blob name to a full URL
    /// </summary>
    /// <param name="blobName">The blob name stored in the database</param>
    /// <returns>Full URL to the image, or null if blob name is null/empty</returns>
    string? GetFullImageUrl(string? blobName);
}

