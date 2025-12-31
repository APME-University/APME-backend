using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;

namespace APME.Customers;

public class CustomerClaimsPrincipalFactory : UserClaimsPrincipalFactory<Customer, IdentityRole>, ITransientDependency
{
    public CustomerClaimsPrincipalFactory(UserManager<Customer> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> options) 
        : base(userManager, roleManager, options)
    {
    }

    [UnitOfWork]
    public override async Task<ClaimsPrincipal> CreateAsync(Customer user)
    {
        var principal = await base.CreateAsync(user);
        var identity = principal.Identities.First();

        if (!user.FirstName.IsNullOrWhiteSpace())
        {
            identity.AddIfNotContains(new Claim(AbpClaimTypes.Name, user.FirstName));
        }

        if (!user.LastName.IsNullOrWhiteSpace())
        {
            identity.AddIfNotContains(new Claim(AbpClaimTypes.SurName, user.LastName));
        }

        identity.AddIfNotContains(new Claim(AbpClaimTypes.EmailVerified, user.EmailConfirmed.ToString()));
        
        if (!user.PhoneNumber.IsNullOrWhiteSpace())
        {
            identity.AddIfNotContains(new Claim(AbpClaimTypes.PhoneNumber, user.PhoneNumber));
        }

        identity.AddIfNotContains(
            new Claim(AbpClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed.ToString()));

        if (!user.Email.IsNullOrWhiteSpace())
        {
            identity.AddIfNotContains(new Claim(AbpClaimTypes.Email, user.Email));
        }

        // Add customer-specific claim for policy-based authorization
        identity.AddIfNotContains(new Claim("is_customer", true.ToString()));

        return principal;
    }
}
