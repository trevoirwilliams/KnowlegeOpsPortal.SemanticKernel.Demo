using System;
using KnowledgeOps.Web.Services;

namespace KnowledgeOps.Web.BackgroundWorkers;

public class DocumentProcessingWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document processing worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();

                var documentProcessingService =
                    scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
                await documentProcessingService.ProcessNextQueuedDocumentAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during graceful shutdown.
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "An error occurred while processing queued documents.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("Document processing worker stopped.");
    }
}
