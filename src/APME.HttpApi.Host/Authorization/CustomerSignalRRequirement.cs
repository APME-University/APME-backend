using Microsoft.AspNetCore.Authorization;

namespace APME.HttpApi.Host.Authorization;

/// <summary>
/// Authorization requirement for Customer SignalR connections.
/// </summary>
public class CustomerSignalRRequirement : IAuthorizationRequirement
{
}

