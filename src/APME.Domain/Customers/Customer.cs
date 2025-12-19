using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace APME.Customers;

public class Customer : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    #region Identity Information
    public bool IsActive { get; set; }
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual DateTimeOffset? LockoutEnd { get; set; }
    [PersonalData]
    public virtual bool TwoFactorEnabled { get; set; }
    [PersonalData]
    public virtual bool PhoneNumberConfirmed { get; set; }
    [ProtectedPersonalData]
    public virtual string? PhoneNumber { get; set; }
    [DisableAuditing]
    public virtual string SecurityStamp { get; protected internal set; }
    public virtual string? PasswordHash { get; set; }
    [PersonalData]
    public virtual bool EmailConfirmed { get; set; }
    public virtual string? NormalizedEmail { get; set; }
    [ProtectedPersonalData]
    public virtual string? Email { get; set; }
    public virtual string? NormalizedUserName { get; set; }
    [ProtectedPersonalData]
    public virtual string? UserName { get; set; }
    public virtual bool LockoutEnabled { get; set; }
    public virtual int AccessFailedCount { get; set; }
    #endregion

    #region Properties
    public Guid? TenantId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    #endregion

    #region Navigation Properties
    public virtual ICollection<CustomerUserRole> Roles { get; protected set; }
    public virtual ICollection<IdentityUserClaim> Claims { get; protected set; }
    public virtual ICollection<IdentityUserLogin> Logins { get; protected set; }
    public virtual ICollection<IdentityUserToken> Tokens { get; protected set; }
    #endregion

    protected Customer()
    {
        Roles = new List<CustomerUserRole>();
        Claims = new List<IdentityUserClaim>();
        Logins = new List<IdentityUserLogin>();
        Tokens = new List<IdentityUserToken>();
    }

    public Customer(
        Guid id,
        Guid? tenantId,
        string firstName,
        string lastName,
        string? email = null,
        string? phoneNumber = null) : base(id)
    {
        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        IsActive = true;
        LockoutEnabled = true;
        AccessFailedCount = 0;
        Roles = new List<CustomerUserRole>();
        Claims = new List<IdentityUserClaim>();
        Logins = new List<IdentityUserLogin>();
        Tokens = new List<IdentityUserToken>();
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

    public virtual void SetPhoneNumberConfirmed(bool confirmed)
    {
        PhoneNumberConfirmed = confirmed;
    }

    public virtual void SetEmailConfirmed(bool confirmed)
    {
        if (this.Email.IsNullOrEmpty())
            return;
        EmailConfirmed = confirmed;
    }

    public void SetLastPasswordChangeTime(DateTime lastPasswordChangeTime)
    {
        this.LastModificationTime = lastPasswordChangeTime;
    }

    public virtual void AddRole(Guid roleId)
    {
        Check.NotNull(roleId, nameof(roleId));

        if (IsInRole(roleId))
        {
            return;
        }
        Roles.Add(new CustomerUserRole(Id, roleId));
    }

    public virtual bool IsInRole(Guid roleId)
    {
        Check.NotNull(roleId, nameof(roleId));

        return Roles.Any(r => r.RoleId == roleId);
    }

    public virtual void RemoveRole(Guid roleId)
    {
        Check.NotNull(roleId, nameof(roleId));

        if (!IsInRole(roleId))
        {
            return;
        }

        Roles.RemoveAll(r => r.RoleId == roleId);
    }

    public virtual IdentityUserClaim FindClaim([NotNull] Claim claim)
    {
        Check.NotNull(claim, nameof(claim));

        return Claims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
    }

    public virtual void ReplaceClaim([NotNull] Claim claim, [NotNull] Claim newClaim)
    {
        Check.NotNull(claim, nameof(claim));
        Check.NotNull(newClaim, nameof(newClaim));

        var userClaims = Claims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type);
        foreach (var userClaim in userClaims)
        {
            userClaim.SetClaim(newClaim);
        }
    }

    public virtual void RemoveClaims([NotNull] IEnumerable<Claim> claims)
    {
        Check.NotNull(claims, nameof(claims));

        foreach (var claim in claims)
        {
            RemoveClaim(claim);
        }
    }

    public virtual void RemoveClaim([NotNull] Claim claim)
    {
        Check.NotNull(claim, nameof(claim));

        Claims.RemoveAll(c => c.ClaimValue == claim.Value && c.ClaimType == claim.Type);
    }

    public virtual void RemoveLogin([NotNull] string loginProvider, [NotNull] string providerKey)
    {
        Check.NotNull(loginProvider, nameof(loginProvider));
        Check.NotNull(providerKey, nameof(providerKey));

        Logins.RemoveAll(userLogin =>
            userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey);
    }

    [CanBeNull]
    public virtual IdentityUserToken FindToken(string loginProvider, string name)
    {
        return Tokens.FirstOrDefault(t => t.LoginProvider == loginProvider && t.Name == name);
    }

    public virtual void RemoveToken(string loginProvider, string name)
    {
        Tokens.RemoveAll(t => t.LoginProvider == loginProvider && t.Name == name);
    }

    public void SetPhoneNumber(string phoneNumber, bool confirmed)
    {
        PhoneNumber = phoneNumber;
        PhoneNumberConfirmed = !phoneNumber.IsNullOrWhiteSpace() && confirmed;
    }

    public virtual void SetIsActive(bool isActive)
    {
        IsActive = isActive;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, UserName = {UserName}";
    }
}

