using APME.Customers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.Controllers;
using Volo.Abp.Security.Claims;

namespace APME.Controllers;

[ApiController]
[RemoteService(true)]
[Route("api/app/customer-auth")]
[AllowAnonymous]
public class CustomerAuthController : TokenController
{
    private readonly CustomerUserManager _customerUserManager;
    private readonly IOptionsMonitor<OpenIddictServerOptions> _oidcOptions;
    private readonly IConfiguration _configuration;
    private readonly CustomerClaimsPrincipalFactory _customerClaimsPrincipalFactory;
    private readonly ICurrentTenant _currentTenant;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IObjectMapper _objectMapper;

    public CustomerAuthController(
        CustomerUserManager customerUserManager,
        IOptionsMonitor<OpenIddictServerOptions> oidcOptions,
        IConfiguration configuration,
        CustomerClaimsPrincipalFactory customerClaimsPrincipalFactory,
        ICurrentTenant currentTenant,
        IGuidGenerator guidGenerator,
        IObjectMapper objectMapper)
    {
        _customerUserManager = customerUserManager;
        _oidcOptions = oidcOptions;
        _configuration = configuration;
        _customerClaimsPrincipalFactory = customerClaimsPrincipalFactory;
        _currentTenant = currentTenant;
        _guidGenerator = guidGenerator;
        _objectMapper = objectMapper;
    }

    [HttpPost]
    [Route("login")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public async Task<ActionResult<CustomerLoginResponseDto>> Login([FromBody] CustomerLoginDto input)
    {
        // Set tenant context if provided
        if (input.TenantId.HasValue)
        {
            using (_currentTenant.Change(input.TenantId.Value))
            {
                return await PerformLogin(input);
            }
        }

        return await PerformLogin(input);
    }

    private async Task<ActionResult<CustomerLoginResponseDto>> PerformLogin(CustomerLoginDto input)
    {
        // Normalize email input (focusing on email/password authentication)
        var normalizedInput = CustomerLoginDto.ConvertArabicToEnglishNumbers(input.EmailOrPhone?.Trim());

        // Try to find customer by email (primary) or phone (fallback)
        Customer customer = null;
        if (normalizedInput.Contains("@"))
        {
            // Email login (primary method)
            customer = await _customerUserManager.FindByEmailAsync(normalizedInput);
        }
        else if (!string.IsNullOrWhiteSpace(normalizedInput))
        {
            // Phone login (fallback - for backward compatibility)
            customer = await _customerUserManager.FindByPhoneNumberAsync(normalizedInput);
        }

        if (customer == null)
        {
            return BadRequest(new { Error = "Invalid email or password." });
        }

        // Verify password
        var passwordValid = await _customerUserManager.CheckPasswordAsync(customer, input.Password);
        if (!passwordValid)
        {
            return BadRequest(new { Error = "Invalid email or password." });
        }

        if (!customer.IsActive)
        {
            return BadRequest(new { Error = "Customer account is inactive." });
        }

        // Clear claims cache
        await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(customer.Id);

        // Create OpenIddict request
        var request = new OpenIddictRequest
        {
            ClientId = "APME_Store",
            Scope = "offline_access APME",
        };

        var principal = await SetSuccessResultForCustomer(request, customer);
        var options = _oidcOptions.CurrentValue;
        var descriptor = new SecurityTokenDescriptor
        {
            Audience = "APME",
            Subject = new ClaimsIdentity(principal.Claims, TokenValidationParameters.DefaultAuthenticationType),
            EncryptingCredentials = options.DisableAccessTokenEncryption
                ? null
                : options.EncryptionCredentials.First(),
            Issuer = _configuration["AuthServer:Authority"],
            SigningCredentials = options.SigningCredentials.First(),
            TokenType = OpenIddictConstants.JsonWebTokenTypes.AccessToken,
            Expires = DateTime.UtcNow.AddDays(30), // 30 days token expiry
        };

        var accessToken = options.JsonWebTokenHandler.CreateToken(descriptor);

        // Map customer to DTO
        var customerDto = _objectMapper.Map<Customer, CustomerDto>(customer);

        return Ok(new CustomerLoginResponseDto
        {
            Customer = customerDto,
            Token = accessToken,
        });
    }

    [HttpPost]
    [Route("register")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public async Task<ActionResult<CustomerLoginResponseDto>> Register([FromBody] CustomerRegisterDto input)
    {
        // Set tenant context if provided
        if (input.TenantId.HasValue)
        {
            using (_currentTenant.Change(input.TenantId.Value))
            {
                return await PerformRegister(input);
            }
        }

        return await PerformRegister(input);
    }

    private async Task<ActionResult<CustomerLoginResponseDto>> PerformRegister(CustomerRegisterDto input)
    {
        // Validate email is provided (required for email/password authentication)
        if (string.IsNullOrWhiteSpace(input.Email))
        {
            return BadRequest(new { Error = "Email is required." });
        }

        // Check if email already exists
        var existingByEmail = await _customerUserManager.FindByEmailAsync(input.Email.Trim());
        if (existingByEmail != null)
        {
            return BadRequest(new { Error = "Email already registered." });
        }

        // Optional: Check phone if provided
        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            var normalizedPhone = CustomerLoginDto.ConvertArabicToEnglishNumbers(input.PhoneNumber);
            var existingByPhone = await _customerUserManager.FindByPhoneNumberAsync(normalizedPhone);
            if (existingByPhone != null)
            {
                return BadRequest(new { Error = "Phone number already registered." });
            }
        }

        // Create customer with email (required)
        // Handle phone number - convert to empty string if null/empty to avoid database constraint issues
        var phoneNumber = !string.IsNullOrWhiteSpace(input.PhoneNumber) 
            ? CustomerLoginDto.ConvertArabicToEnglishNumbers(input.PhoneNumber) 
            : null;
        
        var customer = new Customer(
            _guidGenerator.Create(),
            input.TenantId,
            input.FirstName,
            input.LastName,
            input.Email.Trim(), // Email is required
            phoneNumber); // Phone is optional (can be null)

        // Generate username from email
        customer.UserName = input.Email.Split('@')[0] + Guid.NewGuid().ToString("N")[..8];

        // Create user with password
        var result = await _customerUserManager.CreateAsync(customer, input.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { Error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        // Auto-confirm email (required)
        await _customerUserManager.SetEmailAsync(customer, input.Email.Trim());
        customer.SetEmailConfirmed(true);

        // Auto-confirm phone if provided (optional)
        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            await _customerUserManager.SetPhoneNumberAsync(customer, CustomerLoginDto.ConvertArabicToEnglishNumbers(input.PhoneNumber));
            customer.SetPhoneNumberConfirmed(true);
        }

        // Update customer after setting confirmed status
        await _customerUserManager.UpdateAsync(customer);

        // Login the newly registered customer using email
        var loginDto = new CustomerLoginDto
        {
            EmailOrPhone = input.Email.Trim(), // Use email for login
            Password = input.Password,
            TenantId = input.TenantId,
        };

        return await PerformLogin(loginDto);
    }

    protected override Task<IActionResult> HandleClientCredentialsAsync(OpenIddictRequest request)
    {
        return base.HandleClientCredentialsAsync(request);
    }

    private async Task<ClaimsPrincipal> SetSuccessResultForCustomer(OpenIddictRequest request, Customer customer)
    {
        await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(customer.Id);

        var principal = await _customerClaimsPrincipalFactory.CreateAsync(customer);

        principal.SetScopes(request.GetScopes());
        principal.SetResources(await GetResourcesAsync(request.GetScopes()));

        await HandleCustomerClaimsAsync(request, principal);

        await IdentitySecurityLogManager.SaveAsync(
            new IdentitySecurityLogContext
            {
                Identity = OpenIddictSecurityLogIdentityConsts.OpenIddict,
                Action = OpenIddictSecurityLogActionConsts.LoginSucceeded,
                UserName = customer.UserName,
                ClientId = request.ClientId
            }
        );

        return principal;
    }

    private Task HandleCustomerClaimsAsync(OpenIddictRequest request, ClaimsPrincipal principal)
    {
        var securityStampClaimType = IdentityOptions.Value.ClaimsIdentity.SecurityStampClaimType;

        foreach (var claim in principal.Claims)
        {
            if (claim.Type == AbpClaimTypes.TenantId)
            {
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                continue;
            }

            switch (claim.Type)
            {
                case OpenIddictConstants.Claims.PreferredUsername:
                    claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                    if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                    {
                        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                    }
                    break;

                case JwtRegisteredClaimNames.UniqueName:
                    claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                    if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                    {
                        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                    }
                    break;

                case OpenIddictConstants.Claims.Email:
                    claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                    if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                    {
                        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                    }
                    break;

                case OpenIddictConstants.Claims.Role:
                    claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                    if (principal.HasScope(OpenIddictConstants.Scopes.Roles))
                    {
                        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                    }
                    break;

                default:
                    // Never include the security stamp in the access and identity tokens, as it's a secret value.
                    if (claim.Type != securityStampClaimType)
                    {
                        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                    }
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
