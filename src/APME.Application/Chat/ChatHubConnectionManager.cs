using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Manages SignalR hub connections for chat.
/// Tracks active connections per customer and session.
/// </summary>
public class ChatHubConnectionManager : ITransientDependency
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _customerConnections = new();
    private readonly ConcurrentDictionary<string, Guid> _connectionCustomers = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionSessions = new();
    private readonly ILogger<ChatHubConnectionManager> _logger;

    public ChatHubConnectionManager(ILogger<ChatHubConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a connection for a customer.
    /// </summary>
    public void AddConnection(Guid customerId, string connectionId)
    {
        _customerConnections.AddOrUpdate(
            customerId,
            new HashSet<string> { connectionId },
            (key, existing) =>
            {
                existing.Add(connectionId);
                return existing;
            });

        _connectionCustomers[connectionId] = customerId;
        _connectionSessions[connectionId] = new HashSet<string>();

        _logger.LogDebug(
            "Connection registered: {ConnectionId} for customer {CustomerId}",
            connectionId,
            customerId);
    }

    /// <summary>
    /// Removes a connection.
    /// </summary>
    public void RemoveConnection(string connectionId)
    {
        if (_connectionCustomers.TryRemove(connectionId, out var customerId))
        {
            _customerConnections.AddOrUpdate(
                customerId,
                new HashSet<string>(),
                (key, existing) =>
                {
                    existing.Remove(connectionId);
                    return existing;
                });

            if (_customerConnections.TryGetValue(customerId, out var connections) && connections.Count == 0)
            {
                _customerConnections.TryRemove(customerId, out _);
            }
        }

        _connectionSessions.TryRemove(connectionId, out _);

        _logger.LogDebug("Connection removed: {ConnectionId}", connectionId);
    }

    /// <summary>
    /// Adds a session to a connection.
    /// </summary>
    public void AddSession(string connectionId, string sessionId)
    {
        _connectionSessions.AddOrUpdate(
            connectionId,
            new HashSet<string> { sessionId },
            (key, existing) =>
            {
                existing.Add(sessionId);
                return existing;
            });
    }

    /// <summary>
    /// Removes a session from a connection.
    /// </summary>
    public void RemoveSession(string connectionId, string sessionId)
    {
        if (_connectionSessions.TryGetValue(connectionId, out var sessions))
        {
            sessions.Remove(sessionId);
        }
    }

    /// <summary>
    /// Gets all connection IDs for a customer.
    /// </summary>
    public IEnumerable<string> GetCustomerConnections(Guid customerId)
    {
        if (_customerConnections.TryGetValue(customerId, out var connections))
        {
            return connections.ToList();
        }

        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets the customer ID for a connection.
    /// </summary>
    public Guid? GetCustomerId(string connectionId)
    {
        return _connectionCustomers.TryGetValue(connectionId, out var customerId) ? customerId : null;
    }

    /// <summary>
    /// Gets all sessions for a connection.
    /// </summary>
    public IEnumerable<string> GetConnectionSessions(string connectionId)
    {
        if (_connectionSessions.TryGetValue(connectionId, out var sessions))
        {
            return sessions.ToList();
        }

        return Enumerable.Empty<string>();
    }
}




