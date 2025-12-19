using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace APME.Products;

/// <summary>
/// Normalizes product attributes JSON to ensure consistent formatting
/// </summary>
public static class ProductAttributeNormalizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false, // Compact format for storage
        PropertyNamingPolicy = null // Keep original key names (don't convert to camelCase)
    };

    /// <summary>
    /// Normalizes attributes JSON by removing empty values and ensuring consistent format
    /// </summary>
    /// <param name="attributesJson">JSON string containing attribute values</param>
    /// <returns>Normalized JSON string, or null if no valid attributes remain</returns>
    public static string? NormalizeAttributes(string? attributesJson)
    {
        if (string.IsNullOrWhiteSpace(attributesJson))
        {
            return null;
        }

        try
        {
            var attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(attributesJson);
            
            if (attributes == null || attributes.Count == 0)
            {
                return null;
            }

            // Remove empty values
            var filtered = attributes
                .Where(kvp => !IsEmptyValue(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (filtered.Count == 0)
            {
                return null;
            }

            // Re-serialize to ensure consistent format
            return JsonSerializer.Serialize(filtered, SerializerOptions);
        }
        catch (JsonException)
        {
            // If normalization fails, return original (backward compatibility)
            // This allows the system to handle malformed JSON gracefully
            return attributesJson;
        }
        catch (Exception)
        {
            // For any other exception, return original to maintain backward compatibility
            return attributesJson;
        }
    }

    /// <summary>
    /// Checks if a JSON value is considered empty
    /// </summary>
    private static bool IsEmptyValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Null ||
               value.ValueKind == JsonValueKind.Undefined ||
               (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())) ||
               (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0) ||
               (value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == 0);
    }

    /// <summary>
    /// Migrates legacy attribute formats to the current format (if needed)
    /// Currently, no migration is needed as the format is already flat
    /// </summary>
    public static string? MigrateLegacyAttributes(string? attributesJson)
    {
        if (string.IsNullOrWhiteSpace(attributesJson))
        {
            return null;
        }

        try
        {
            var attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(attributesJson);
            
            // Current format is already flat, so no migration needed
            // This method can be extended in the future if format changes are needed
            
            return NormalizeAttributes(attributesJson);
        }
        catch
        {
            // If migration fails, return original
            return attributesJson;
        }
    }
}
