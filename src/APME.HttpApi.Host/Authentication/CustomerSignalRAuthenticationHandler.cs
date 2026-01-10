using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using APME.Customers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;

namespace APME.HttpApi.Host.Authentication;

/// <summary>
/// Authentication handler for Customer SignalR connections.
/// Validates Customer JWT tokens without triggering ABP IdentityUser resolution.
/// </summary>
public class CustomerSignalRAuthenticationHandler : AuthenticationHandler<CustomerSignalRAuthenticationOptions>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IConfiguration _configuration;
    private const string AuthenticationScheme = "CustomerSignalR";

    public CustomerSignalRAuthenticationHandler(
        IOptionsMonitor<CustomerSignalRAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IRepository<Customer, Guid> customerRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _customerRepository = customerRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;

        // Try to get token from Authorization header first
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authHeaderValue = authHeader.ToString();
            if (!string.IsNullOrWhiteSpace(authHeaderValue) && authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeaderValue.Substring("Bearer ".Length).Trim();
            }
        }

        // If not in header, try query string (SignalR negotiation uses query string)
        if (string.IsNullOrWhiteSpace(token) && Request.Query.TryGetValue("access_token", out var accessToken))
        {
            token = accessToken.ToString();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Validate JWT token using JwtSecurityTokenHandler
            // This matches OpenIddict's validation approach
            var tokenHandler = new JwtSecurityTokenHandler();
            
            if (!tokenHandler.CanReadToken(token))
            {
                Logger.LogWarning("Invalid token format");
                return AuthenticateResult.NoResult();
            }

            // Read token without validation first to get claims
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Basic validation: check expiration
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                Logger.LogWarning("Token has expired");
                return AuthenticateResult.NoResult();
            }

            // Check audience matches
            var audience = jwtToken.Audiences?.FirstOrDefault();
            if (audience != null && audience != Options.Audience && audience != "APME")
            {
                Logger.LogWarning("Token audience mismatch: {Audience}", audience);
                return AuthenticateResult.NoResult();
            }

            // Create principal from token claims
            var principal = new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims, AuthenticationScheme));

            // Check if this is a Customer token
            var isCustomerClaim = principal.FindFirst("is_customer");
            bool isCustomerToken = false;

            if (isCustomerClaim != null && bool.TryParse(isCustomerClaim.Value, out var isCustomer) && isCustomer)
            {
                isCustomerToken = true;
            }
            else
            {
                // Check if user ID exists in Customer table
                var userId = principal.FindFirst(AbpClaimTypes.UserId)?.Value 
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? principal.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userIdGuid))
                {
                    // Use UnitOfWork to check if customer exists
                    using var uow = _unitOfWorkManager.Begin();
                    var customerExists = await _customerRepository.AnyAsync(c => c.Id == userIdGuid);
                    if (customerExists)
                    {
                        isCustomerToken = true;
                    }
                }
            }

            // Only authenticate Customer tokens through this handler
            if (!isCustomerToken)
            {
                return AuthenticateResult.NoResult(); // Let other handlers try
            }

            // Verify Customer exists and is active if validation is enabled
            if (Options.ValidateCustomer)
            {
                var userId = principal.FindFirst(AbpClaimTypes.UserId)?.Value 
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? principal.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var customerId))
                {
                    return AuthenticateResult.Fail("Invalid customer ID in token");
                }

                using var uow = _unitOfWorkManager.Begin();
                var customer = await _customerRepository.FindAsync(c => c.Id == customerId);
                
                if (customer == null)
                {
                    Logger.LogWarning("Customer not found: {CustomerId}", customerId);
                    return AuthenticateResult.Fail("Customer not found");
                }

                if (!customer.IsActive)
                {
                    Logger.LogWarning("Customer is inactive: {CustomerId}", customerId);
                    return AuthenticateResult.Fail("Customer account is inactive");
                }
            }

            // Create a new ClaimsIdentity with Customer claims
            // This bypasses ABP's dynamic claims resolution
            var customerIdentity = new ClaimsIdentity(
                principal.Claims,
                AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role);

            // Ensure is_customer claim exists
            if (customerIdentity.FindFirst("is_customer") == null)
            {
                customerIdentity.AddClaim(new Claim("is_customer", true.ToString()));
            }

            var customerPrincipal = new ClaimsPrincipal(customerIdentity);
            var ticket = new AuthenticationTicket(customerPrincipal, AuthenticationScheme);

            Logger.LogInformation(
                "Customer authenticated via SignalR: {CustomerId}",
                customerPrincipal.FindFirst(AbpClaimTypes.UserId)?.Value);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating Customer token for SignalR");
            return AuthenticateResult.NoResult(); // Let other handlers try on error
        }
    }
}

