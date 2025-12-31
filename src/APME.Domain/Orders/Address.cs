using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace APME.Orders;

/// <summary>
/// Value Object representing a physical address
/// Immutable after creation
/// </summary>
public class Address : ValueObject
{
    /// <summary>
    /// Full name of the recipient
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Street address line 1
    /// </summary>
    public string Street { get; private set; }

    /// <summary>
    /// Street address line 2 (apartment, suite, etc.)
    /// </summary>
    public string? Street2 { get; private set; }

    /// <summary>
    /// City name
    /// </summary>
    public string City { get; private set; }

    /// <summary>
    /// State or province
    /// </summary>
    public string? State { get; private set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    public string PostalCode { get; private set; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    public string Country { get; private set; }

    /// <summary>
    /// Phone number for delivery contact
    /// </summary>
    public string? Phone { get; private set; }

    protected Address()
    {
        // Required by EF Core
    }

    public Address(
        string fullName,
        string street,
        string city,
        string postalCode,
        string country,
        string? street2 = null,
        string? state = null,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required", nameof(fullName));
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required", nameof(postalCode));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        FullName = fullName.Trim();
        Street = street.Trim();
        Street2 = street2?.Trim();
        City = city.Trim();
        State = state?.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim().ToUpperInvariant();
        Phone = phone?.Trim();
    }

    /// <summary>
    /// Returns the address as a formatted multi-line string
    /// </summary>
    public string ToFormattedString()
    {
        var lines = new List<string> { FullName, Street };
        
        if (!string.IsNullOrWhiteSpace(Street2))
            lines.Add(Street2);
        
        var cityLine = !string.IsNullOrWhiteSpace(State) 
            ? $"{City}, {State} {PostalCode}" 
            : $"{City} {PostalCode}";
        lines.Add(cityLine);
        
        lines.Add(Country);
        
        if (!string.IsNullOrWhiteSpace(Phone))
            lines.Add($"Phone: {Phone}");

        return string.Join(Environment.NewLine, lines);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FullName;
        yield return Street;
        yield return Street2 ?? string.Empty;
        yield return City;
        yield return State ?? string.Empty;
        yield return PostalCode;
        yield return Country;
        yield return Phone ?? string.Empty;
    }
}

