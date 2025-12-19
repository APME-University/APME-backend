using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace APME.Customers;

public class CustomerUserManager : UserManager<Customer>, IDomainService, ITransientDependency
{
    private readonly IRepository<Customer> Repository;

    public CustomerUserManager(
        IUserStore<Customer> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<Customer> passwordHasher,
        IEnumerable<IUserValidator<Customer>> userValidators,
        IEnumerable<IPasswordValidator<Customer>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<Customer>> logger,
        IRepository<Customer> repository)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        Repository = repository;
    }

    public virtual async Task<Customer> FindByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Repository.FirstOrDefaultAsync(x => x.PhoneNumber.Replace(" ", "") == phoneNumber.Replace(" ", ""), cancellationToken);
    }

    public virtual async Task<Customer> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Repository.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public override async Task<IdentityResult> CreateAsync(Customer user)
    {
        ThrowIfDisposed();
        await UpdateSecurityStampAsync(user).ConfigureAwait(false);
        var result = await ValidateUserAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return result;
        }
        if (Options.Lockout.AllowedForNewUsers && SupportsUserLockout)
        {
            await GetUserLockoutStore().SetLockoutEnabledAsync(user, true, CancellationToken).ConfigureAwait(false);
        }
        await UpdateNormalizedUserNameAsync(user).ConfigureAwait(false);
        if (!user.Email.IsNullOrEmpty())
        {
            await UpdateNormalizedEmailAsync(user).ConfigureAwait(false);
        }
        return await Store.CreateAsync(user, CancellationToken).ConfigureAwait(false);
    }

    private IUserLockoutStore<Customer> GetUserLockoutStore()
    {
        var cast = Store as IUserLockoutStore<Customer>;
        if (cast == null)
        {
            throw new NotSupportedException("NotSupportedLockoutStore");
        }
        return cast;
    }
}
