using System.ComponentModel.DataAnnotations;

namespace APME.Shops;

public class CreateUpdateShopDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(256)]
    public string Slug { get; set; }

    public bool IsActive { get; set; }

    [StringLength(512)]
    public string? LogoUrl { get; set; }

    public string? Settings { get; set; }

    // Tenant creation properties (required for create, optional for update)
    [StringLength(64)]
    public string? TenantName { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? AdminEmail { get; set; }

    [StringLength(128)]
    public string? AdminPassword { get; set; }

    [StringLength(256)]
    public string? AdminUserName { get; set; }

    [StringLength(64)]
    public string? AdminFirstName { get; set; }

    [StringLength(64)]
    public string? AdminLastName { get; set; }
}

