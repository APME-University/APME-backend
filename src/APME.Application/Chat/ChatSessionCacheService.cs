using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Cache service for chat sessions and recent messages.
/// Improves performance by reducing database round trips.
/// </summary>
public class ChatSessionCacheService : ITransientDependency
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ChatSessionCacheService> _logger;

    private const string SessionCacheKeyPrefix = "chat:session:";
    private const string MessagesCacheKeyPrefix = "chat:messages:";
    private static readonly TimeSpan SessionCacheTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan MessagesCacheTtl = TimeSpan.FromMinutes(30);

    public ChatSessionCacheService(
        IDistributedCache cache,
        ILogger<ChatSessionCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets a cached session.
    /// </summary>
    public async Task<ChatSessionDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetSessionCacheKey(sessionId);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached == null)
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ChatSessionDto>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Caches a session.
    /// </summary>
    public async Task SetSessionAsync(
        ChatSessionDto session,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetSessionCacheKey(session.Id);
        var json = System.Text.Json.JsonSerializer.Serialize(session);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SessionCacheTtl
        };

        await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
    }

    /// <summary>
    /// Invalidates session cache.
    /// </summary>
    public async Task InvalidateSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetSessionCacheKey(sessionId);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    /// <summary>
    /// Gets cached recent messages for a session.
    /// </summary>
    public async Task<System.Collections.Generic.List<ChatMessageResponseDto>?> GetRecentMessagesAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetMessagesCacheKey(sessionId);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached == null)
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ChatMessageResponseDto>>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached messages for session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Caches recent messages for a session.
    /// </summary>
    public async Task SetRecentMessagesAsync(
        Guid sessionId,
        System.Collections.Generic.List<ChatMessageResponseDto> messages,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetMessagesCacheKey(sessionId);
        var json = System.Text.Json.JsonSerializer.Serialize(messages);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = MessagesCacheTtl
        };

        await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
    }

    /// <summary>
    /// Invalidates messages cache for a session.
    /// Called when a new message is added.
    /// </summary>
    public async Task InvalidateMessagesAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetMessagesCacheKey(sessionId);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private static string GetSessionCacheKey(Guid sessionId)
    {
        return $"{SessionCacheKeyPrefix}{sessionId}";
    }

    private static string GetMessagesCacheKey(Guid sessionId)
    {
        return $"{MessagesCacheKeyPrefix}{sessionId}";
    }
}




