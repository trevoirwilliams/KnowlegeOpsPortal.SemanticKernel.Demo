using System;
using KnowledgeOps.Web.Models.Copilot;
using KnowledgeOps.Web.Services;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Web.BackgroundWorkers;

public class CopilotHistoryCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<CopilotHistoryOptions> options,
    ILogger<CopilotHistoryCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);
        using var timer = new PeriodicTimer(
            TimeSpan.FromDays(options.Value.CleanupIntervalDays));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var conversationService = scope.ServiceProvider
                .GetRequiredService<ICopilotConversationService>();

            var deletedCount = await conversationService.DeleteConversationsOlderThanAsync(
                    TimeSpan.FromDays(options.Value.RetentionDays),
                    stoppingToken);
            
            if (deletedCount > 0)
            {
                logger.LogInformation(
                    "Deleted {DeletedCount} stale copilot conversations.",
                    deletedCount);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during application shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while cleaning up old copilot conversations.");
        }
    }
}
