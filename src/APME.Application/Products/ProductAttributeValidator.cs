using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Volo.Abp;

namespace APME.Products;

/// <summary>
/// Validates product attributes against their definitions
/// </summary>
public static class ProductAttributeValidator
{
    /// <summary>
    /// Validates attributes JSON against ProductAttribute definitions
    /// </summary>
    /// <param name="attributesJson">JSON string containing attribute values</param>
    /// <param name="attributeDefinitions">List of ProductAttribute definitions for the shop</param>
    /// <returns>Validation result with errors if any</returns>
    public static ValidationResult ValidateAttributes(
        string? attributesJson,
        List<ProductAttributeDto> attributeDefinitions)
    {
        if (string.IsNullOrWhiteSpace(attributesJson))
        {
            return ValidationResult.Success; // Empty attributes are allowed
        }

        if (attributeDefinitions == null || attributeDefinitions.Count == 0)
        {
            // No definitions means no validation needed (backward compatibility)
            return ValidationResult.Success;
        }

        try
        {
            var attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(attributesJson);
            
            if (attributes == null || attributes.Count == 0)
            {
                return ValidationResult.Success;
            }

            var errors = new List<string>();

            foreach (var definition in attributeDefinitions)
            {
                var hasValue = attributes.TryGetValue(definition.Name, out var value);
                
                // Check required attributes
                if (definition.IsRequired && (!hasValue || IsEmptyValue(value)))
                {
                    errors.Add($"{definition.DisplayName} is required");
                    continue;
                }

                // Skip validation if value is empty and not required
                if (!hasValue || IsEmptyValue(value))
                {
                    continue;
                }

                // Type validation
                var typeError = ValidateAttributeType(definition, value);
                if (!string.IsNullOrEmpty(typeError))
                {
                    errors.Add($"{definition.DisplayName}: {typeError}");
                }
            }

            // Optional: Check for unknown attributes (commented out for flexibility)
            // This allows shops to add custom attributes without defining them first
            // Uncomment if you want strict validation:
            // foreach (var key in attributes.Keys)
            // {
            //     if (!attributeDefinitions.Any(d => d.Name == key))
            //     {
            //         errors.Add($"Unknown attribute: {key}");
            //     }
            // }

            return errors.Count > 0
                ? ValidationResult.Failed(errors.ToArray())
                : ValidationResult.Success;
        }
        catch (JsonException ex)
        {
            return ValidationResult.Failed($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failed($"Validation error: {ex.Message}");
        }
    }

    private static bool IsEmptyValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Null ||
               value.ValueKind == JsonValueKind.Undefined ||
               (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())) ||
               (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0) ||
               (value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == 0);
    }

    private static string? ValidateAttributeType(ProductAttributeDto definition, JsonElement value)
    {
        return definition.DataType switch
        {
            ProductAttributeDataType.Number => ValidateNumber(value),
            ProductAttributeDataType.Boolean => ValidateBoolean(value),
            ProductAttributeDataType.Date => ValidateDate(value),
            ProductAttributeDataType.Text => null, // Text accepts any value
            _ => null
        };
    }

    private static string? ValidateNumber(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            return "Must be a number";
        }

        // Additional check: ensure it's a valid number
        try
        {
            value.GetDouble();
        }
        catch
        {
            return "Must be a valid number";
        }

        return null;
    }

    private static string? ValidateBoolean(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
        {
            return "Must be true or false";
        }

        return null;
    }

    private static string? ValidateDate(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return "Must be a date string (YYYY-MM-DD)";
        }

        var dateStr = value.GetString();
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return "Date cannot be empty";
        }

        // Validate ISO date format (YYYY-MM-DD)
        if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return "Must be in YYYY-MM-DD format";
        }

        return null;
    }
}

/// <summary>
/// Represents the result of attribute validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();

    public static ValidationResult Success => new() { IsValid = true };
    
    public static ValidationResult Failed(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors 
    };
}
