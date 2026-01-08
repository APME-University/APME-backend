using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using APME.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;

namespace APME.Controllers;

/// <summary>
/// API controller for chat sessions and messages.
/// Provides REST endpoints for chat management.
/// </summary>
[Route("api/app/chat")]
[Authorize(AuthenticationSchemes = "OpenIddict.Validation.AspNetCore")]
public class ChatController : AbpController
{
    private readonly IChatOrchestratorService _orchestrator;
    private readonly ICurrentUser _currentUser;

    public ChatController(
        IChatOrchestratorService orchestrator,
        ICurrentUser currentUser)
    {
        _orchestrator = orchestrator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets the customer ID from claims, supporting both customer and admin tokens.
    /// </summary>
    private Guid GetCustomerId()
    {
        // Try ICurrentUser first (works for admin tokens)
        if (_currentUser.Id.HasValue)
        {
            return _currentUser.Id.Value;
        }

        // For customer tokens, extract from claims directly
        var userIdClaim = User?.FindFirst(AbpClaimTypes.UserId)
            ?? User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? User?.FindFirst("sub");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var customerId))
        {
            return customerId;
        }

        throw new UserFriendlyException("User not authenticated");
    }

    /// <summary>
    /// Gets or creates a chat session for the current customer.
    /// </summary>
    [HttpPost("session")]
    public async Task<ChatSessionDto> GetOrCreateSessionAsync()
    {
        var customerId = GetCustomerId();
        return await _orchestrator.GetOrCreateSessionAsync(customerId);
    }

    /// <summary>
    /// Gets a chat session by ID.
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<ChatSessionDto?> GetSessionAsync(Guid sessionId)
    {
        var customerId = GetCustomerId();
        return await _orchestrator.GetSessionAsync(sessionId, customerId);
    }

    /// <summary>
    /// Gets recent messages for a session.
    /// </summary>
    [HttpGet("session/{sessionId}/messages")]
    public async Task<List<ChatMessageResponseDto>> GetMessagesAsync(
        Guid sessionId,
        [FromQuery] int count = 50)
    {
        var customerId = GetCustomerId();
        return await _orchestrator.GetRecentMessagesAsync(sessionId, customerId, count);
    }

    /// <summary>
    /// Gets all sessions for the current customer (including archived).
    /// </summary>
    [HttpGet("sessions")]
    public async Task<List<ChatSessionDto>> GetAllSessionsAsync(
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 50)
    {
        var customerId = GetCustomerId();
        return await _orchestrator.GetAllSessionsAsync(customerId, skipCount, maxResultCount);
    }
}

