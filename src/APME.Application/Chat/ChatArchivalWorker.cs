using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Background worker for archiving old chat messages.
/// Runs daily to archive messages older than the retention period.
/// </summary>
public class ChatArchivalWorker : ITransientDependency
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatArchivalWorker> _logger;

    public ChatArchivalWorker(
        IChatMessageRepository messageRepository,
        IOptions<ChatOptions> options,
        ILogger<ChatArchivalWorker> logger)
    {
        _messageRepository = messageRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Archives messages older than the retention period.
    /// This method is called by Hangfire on a schedule.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ArchiveOldMessagesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting chat message archival job");

        try
        {
            var archiveBeforeDate = DateTime.UtcNow.AddDays(-_options.MessageRetentionDays);
            var batchSize = 1000;
            var totalArchived = 0;

            while (true)
            {
                var messagesToArchive = await _messageRepository.GetMessagesToArchiveAsync(
                    archiveBeforeDate,
                    batchSize,
                    cancellationToken);

                if (messagesToArchive.Count == 0)
                {
                    break;
                }

                foreach (var message in messagesToArchive)
                {
                    message.Archive();
                    await _messageRepository.UpdateAsync(message, cancellationToken: cancellationToken);
                }

                totalArchived += messagesToArchive.Count;

                _logger.LogInformation(
                    "Archived batch of {Count} messages. Total archived: {Total}",
                    messagesToArchive.Count,
                    totalArchived);

                // If we got fewer than batch size, we're done
                if (messagesToArchive.Count < batchSize)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Chat message archival completed. Total messages archived: {Total}",
                totalArchived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat message archival");
            throw;
        }
    }
}




