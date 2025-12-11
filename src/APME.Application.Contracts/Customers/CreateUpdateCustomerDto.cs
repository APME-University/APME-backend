using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Customers;

public class CreateUpdateCustomerDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(128)]
    public string FirstName { get; set; }

    [Required]
    [StringLength(128)]
    public string LastName { get; set; }

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool IsActive { get; set; }
}

