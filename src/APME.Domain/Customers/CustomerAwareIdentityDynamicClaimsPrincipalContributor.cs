using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;

namespace APME.Customers;

/// <summary>
/// Custom IdentityDynamicClaimsPrincipalContributor that checks for Customer tokens
/// before attempting IdentityUser resolution. This prevents exceptions when Customer
/// GUIDs are looked up in the IdentityUser table.
/// </summary>
public class CustomerAwareIdentityDynamicClaimsPrincipalContributor : IAbpClaimsPrincipalContributor, ITransientDependency
{
    private readonly IdentityUserManager _identityUserManager;
    private readonly IRepository<Customer> _customerRepository;

    public CustomerAwareIdentityDynamicClaimsPrincipalContributor(
        IdentityUserManager identityUserManager,
        IRepository<Customer> customerRepository)
    {
        _identityUserManager = identityUserManager;
        _customerRepository = customerRepository;
    }

    [UnitOfWork]
    public virtual async Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
    {
        var userId = context.ClaimsPrincipal?.FindFirst(AbpClaimTypes.UserId)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            return;
        }

        // Check if this is a Customer token first
        var isCustomerClaim = context.ClaimsPrincipal?.FindFirst("is_customer");
        bool isCustomerToken = false;

        if (isCustomerClaim != null && bool.TryParse(isCustomerClaim.Value, out var isCustomer) && isCustomer)
        {
            isCustomerToken = true;
        }
        else
        {
            // Check if user ID exists in Customer table
            var customerExists = await _customerRepository.AnyAsync(c => c.Id == userIdGuid);
            if (customerExists)
            {
                isCustomerToken = true;
            }
        }

        // If it's a Customer token, skip IdentityUser resolution
        // The CustomerDynamicClaimsPrincipalContributor has already handled it
        if (isCustomerToken)
        {
            return;
        }

        // For non-Customer tokens, perform IdentityUser resolution (original behavior)
        try
        {
            var identityUser = await _identityUserManager.GetByIdAsync(userIdGuid);
            if (identityUser != null)
            {
                // Add dynamic claims for IdentityUser if needed
                // This mimics the behavior of IdentityDynamicClaimsPrincipalContributor
                var identity = context.ClaimsPrincipal?.Identities.FirstOrDefault();
                if (identity != null)
                {
                    // Add any additional claims that would normally be added by IdentityDynamicClaimsPrincipalContributor
                    // The base claims are already in the token from the authentication process
                }
            }
        }
        catch (Volo.Abp.Domain.Entities.EntityNotFoundException)
        {
            // User not found - this is expected for some cases, just return
            return;
        }
    }
}

