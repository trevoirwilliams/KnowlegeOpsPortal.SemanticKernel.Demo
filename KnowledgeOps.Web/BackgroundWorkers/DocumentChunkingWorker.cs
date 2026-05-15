using System;
using KnowledgeOps.Web.Services;

namespace KnowledgeOps.Web.BackgroundWorkers;

public class DocumentChunkingWorkerService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DocumentChunkingWorkerService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    protected override  async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document chunking worker started.");

        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                IDocumentChunkingService chunkingService = scope.ServiceProvider.GetRequiredService<IDocumentChunkingService>();

                bool result = await chunkingService.ChunkNextTextExtractedDocumentAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while chunking documents.");
                Task.Delay(PollingInterval, stoppingToken).Wait(stoppingToken);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }
}
