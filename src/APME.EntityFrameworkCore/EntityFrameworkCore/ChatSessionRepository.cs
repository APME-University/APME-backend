using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace APME.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of IChatSessionRepository
/// </summary>
public class ChatSessionRepository : EfCoreRepository<APMEDbContext, ChatSession, Guid>, IChatSessionRepository
{
    public ChatSessionRepository(IDbContextProvider<APMEDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<ChatSession> GetOrCreateActiveSessionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        var activeSession = await dbContext.ChatSessions
            .Where(s => s.CustomerId == customerId && s.Status == ChatSessionStatus.Active)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSession != null)
        {
            return activeSession;
        }

        // Create new session
        var newSession = new ChatSession(GuidGenerator.Create(), customerId);
        await dbContext.ChatSessions.AddAsync(newSession, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return newSession;
    }

    public async Task<ChatSession?> GetByCustomerAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatSessions
            .FirstOrDefaultAsync(
                s => s.Id == sessionId && s.CustomerId == customerId,
                cancellationToken);
    }

    public async Task<List<ChatSession>> GetActiveSessionsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatSessions
            .Where(s => s.CustomerId == customerId && s.Status == ChatSessionStatus.Active)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatSession>> GetAllSessionsAsync(
        Guid customerId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatSessions
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.LastActivityAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveSessionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatSessions
            .AnyAsync(
                s => s.CustomerId == customerId && s.Status == ChatSessionStatus.Active,
                cancellationToken);
    }
}




