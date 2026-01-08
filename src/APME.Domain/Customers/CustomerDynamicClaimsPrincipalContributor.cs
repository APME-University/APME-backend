using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;

namespace APME.Customers;

/// <summary>
/// Claims principal contributor for Customer authentication.
/// Detects Customer tokens and prevents IdentityUser lookup to avoid conflicts.
/// This contributor must run before IdentityDynamicClaimsPrincipalContributor to prevent
/// exceptions when Customer GUIDs are looked up in the IdentityUser table.
/// </summary>
public class CustomerDynamicClaimsPrincipalContributor : IAbpClaimsPrincipalContributor, ITransientDependency
{
    private readonly IRepository<Customer> _customerRepository;

    public CustomerDynamicClaimsPrincipalContributor(IRepository<Customer> customerRepository)
    {
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

        // Check if this is a Customer token by looking for the is_customer claim
        var isCustomerClaim = context.ClaimsPrincipal?.FindFirst("is_customer");
        bool isCustomerToken = false;

        if (isCustomerClaim != null && bool.TryParse(isCustomerClaim.Value, out var isCustomer) && isCustomer)
        {
            isCustomerToken = true;
        }
        else
        {
            // Check if user ID exists in Customer table
            // This handles cases where is_customer claim might be missing from token
            var customerExists = await _customerRepository.AnyAsync(c => c.Id == userIdGuid);
            if (customerExists)
            {
                isCustomerToken = true;
                // Add the is_customer claim if missing
                var identity = context.ClaimsPrincipal?.Identities.FirstOrDefault();
                if (identity != null && isCustomerClaim == null)
                {
                    identity.AddClaim(new Claim("is_customer", true.ToString()));
                }
            }
        }

        if (isCustomerToken)
        {
            // Verify the Customer exists and is active
            var customer = await _customerRepository.FindAsync(c => c.Id == userIdGuid);
            if (customer != null && customer.IsActive)
            {
                // This is a valid Customer token - ensure is_customer claim exists
                var identity = context.ClaimsPrincipal?.Identities.FirstOrDefault();
                if (identity != null)
                {
                    // Ensure is_customer claim exists
                    if (isCustomerClaim == null)
                    {
                        identity.AddClaim(new Claim("is_customer", true.ToString()));
                    }
                }
            }
        }

        // If not a Customer token, let IdentityDynamicClaimsPrincipalContributor handle it normally
    }
}

