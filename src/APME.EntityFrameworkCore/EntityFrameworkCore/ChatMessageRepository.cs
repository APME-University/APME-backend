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
/// EF Core implementation of IChatMessageRepository
/// </summary>
public class ChatMessageRepository : EfCoreRepository<APMEDbContext, ChatMessage, Guid>, IChatMessageRepository
{
    public ChatMessageRepository(IDbContextProvider<APMEDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<List<ChatMessage>> GetBySessionAsync(
        Guid sessionId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        var query = dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId);

        if (!includeArchived)
        {
            query = query.Where(m => !m.IsArchived);
        }

        return await query
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(
        Guid sessionId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId && !m.IsArchived)
            .OrderByDescending(m => m.SequenceNumber)
            .Take(count)
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatMessage>> GetMessagesInRangeAsync(
        Guid sessionId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId
                        && !m.IsArchived
                        && m.CreationTime >= startDate
                        && m.CreationTime <= endDate)
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextSequenceNumberAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        var maxSequence = await dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .Select(m => (int?)m.SequenceNumber)
            .MaxAsync(cancellationToken);

        return (maxSequence ?? 0) + 1;
    }

    public async Task<int> GetMessageCountAsync(
        Guid sessionId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        var query = dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId);

        if (!includeArchived)
        {
            query = query.Where(m => !m.IsArchived);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<ChatMessage>> GetMessagesToArchiveAsync(
        DateTime archiveBeforeDate,
        int maxCount = 1000,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ChatMessages
            .Where(m => !m.IsArchived
                        && m.CreationTime < archiveBeforeDate)
            .OrderBy(m => m.CreationTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }
}




