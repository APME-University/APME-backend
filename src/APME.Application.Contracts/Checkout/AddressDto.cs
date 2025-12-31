using System.ComponentModel.DataAnnotations;

namespace APME.Checkout;

/// <summary>
/// DTO for address input
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Full name of the recipient
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Street address line 1
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Street address line 2 (apartment, suite, etc.)
    /// </summary>
    [MaxLength(256)]
    public string? Street2 { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    [MaxLength(128)]
    public string? State { get; set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Phone number for delivery contact
    /// </summary>
    [MaxLength(32)]
    public string? Phone { get; set; }
}

