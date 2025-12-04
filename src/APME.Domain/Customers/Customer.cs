using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Customers;

public class Customer : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid UserId { get; set; } // FK to IdentityUser

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool IsActive { get; set; }

    protected Customer()
    {
        // Required by EF Core
    }

    public Customer(
        Guid id,
        Guid? tenantId,
        Guid userId,
        string firstName,
        string lastName) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        IsActive = true;
    }

    public void UpdateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be null or empty", nameof(firstName));
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be null or empty", nameof(lastName));
        }
        FirstName = firstName;
        LastName = lastName;
    }

    public void UpdatePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }
}

