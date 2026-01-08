using Microsoft.AspNetCore.Authentication;

namespace APME.HttpApi.Host.Authentication;

/// <summary>
/// Options for Customer SignalR authentication.
/// </summary>
public class CustomerSignalRAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Expected audience for Customer tokens.
    /// </summary>
    public string Audience { get; set; } = "APME";

    /// <summary>
    /// Whether to validate the Customer exists and is active.
    /// </summary>
    public bool ValidateCustomer { get; set; } = true;
}

