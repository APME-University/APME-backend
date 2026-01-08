using System;
using System.Linq;
using System.Threading.Tasks;
using APME.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;

namespace APME.HttpApi.Host.Authorization;

/// <summary>
/// Authorization handler for Customer SignalR connections.
/// Validates that the authenticated user is a Customer and is active.
/// </summary>
public class CustomerSignalRAuthorizationHandler : AuthorizationHandler<CustomerSignalRRequirement>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly ILogger<CustomerSignalRAuthorizationHandler> _logger;

    public CustomerSignalRAuthorizationHandler(
        IRepository<Customer, Guid> customerRepository,
        ILogger<CustomerSignalRAuthorizationHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomerSignalRRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Authorization failed: User is not authenticated");
            return;
        }

        // Check if this is a Customer token
        var isCustomerClaim = context.User.FindFirst("is_customer");
        if (isCustomerClaim == null || !bool.TryParse(isCustomerClaim.Value, out var isCustomer) || !isCustomer)
        {
            _logger.LogWarning("Authorization failed: Token is not a Customer token");
            return;
        }

        // Get Customer ID from claims
        var userId = context.User.FindFirst(AbpClaimTypes.UserId)?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var customerId))
        {
            _logger.LogWarning("Authorization failed: Invalid Customer ID in claims");
            return;
        }

        // Verify Customer exists and is active
        try
        {
            // Get UnitOfWorkManager from the resource (HttpContext)
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("Authorization failed: HttpContext not available");
                return;
            }

            var uowManager = httpContext.RequestServices.GetRequiredService<Volo.Abp.Uow.IUnitOfWorkManager>();
            using var uow = uowManager.Begin();

            var customer = await _customerRepository.FindAsync(c => c.Id == customerId);
            
            if (customer == null)
            {
                _logger.LogWarning("Authorization failed: Customer not found: {CustomerId}", customerId);
                return;
            }

            if (!customer.IsActive)
            {
                _logger.LogWarning("Authorization failed: Customer is inactive: {CustomerId}", customerId);
                return;
            }

            // All checks passed
            context.Succeed(requirement);
            _logger.LogInformation("Customer SignalR authorization succeeded: {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Customer SignalR authorization");
            return;
        }
    }
}

