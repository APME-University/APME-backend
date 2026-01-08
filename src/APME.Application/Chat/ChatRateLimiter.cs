using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Simple rate limiter for chat messages per customer.
/// Uses in-memory sliding window.
/// </summary>
public class ChatRateLimiter : ITransientDependency
{
    private readonly ChatOptions _options;
    private readonly ILogger<ChatRateLimiter> _logger;
    private readonly ConcurrentDictionary<Guid, RateLimitWindow> _windows = new();

    public ChatRateLimiter(
        IOptions<ChatOptions> options,
        ILogger<ChatRateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a customer can send a message (rate limit check).
    /// </summary>
    public bool CanSendMessage(Guid customerId)
    {
        var window = _windows.GetOrAdd(customerId, _ => new RateLimitWindow());

        lock (window)
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            // Remove old timestamps
            window.Timestamps.RemoveAll(t => t < oneMinuteAgo);

            // Check if under limit
            if (window.Timestamps.Count >= _options.RateLimitMessagesPerMinute)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for customer {CustomerId}. Count: {Count}, Limit: {Limit}",
                    customerId,
                    window.Timestamps.Count,
                    _options.RateLimitMessagesPerMinute);
                return false;
            }

            // Add current timestamp
            window.Timestamps.Add(now);
            return true;
        }
    }

    /// <summary>
    /// Cleans up old rate limit windows (called periodically).
    /// </summary>
    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new System.Collections.Generic.List<Guid>();

        foreach (var kvp in _windows)
        {
            lock (kvp.Value)
            {
                var oneMinuteAgo = now.AddMinutes(-1);
                kvp.Value.Timestamps.RemoveAll(t => t < oneMinuteAgo);

                if (kvp.Value.Timestamps.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _windows.TryRemove(key, out _);
        }
    }

    private class RateLimitWindow
    {
        public System.Collections.Generic.List<DateTime> Timestamps { get; } = new();
    }
}




