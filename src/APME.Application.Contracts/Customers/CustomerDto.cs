using System;
using Volo.Abp.Application.Dtos;

namespace APME.Customers;

public class CustomerDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool IsActive { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

