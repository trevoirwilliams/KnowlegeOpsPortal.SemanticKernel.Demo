using System;
using KnowledgeOps.Web.Services;

namespace KnowledgeOps.Web.BackgroundWorkers;

public class DocumentEmbeddingWorkerService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentEmbeddingWorkerService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document embedding worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();

                IDocumentEmbeddingService embeddingService = scope
                    .ServiceProvider
                    .GetRequiredService<IDocumentEmbeddingService>();

                bool processedDocument = await embeddingService
                    .EmbedNextChunkedDocumentAsync(stoppingToken);

            }
            catch (OperationCanceledException)
            {
                // Application shutdown.
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "An unexpected error occurred in the document embedding worker.");
            }
            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("Document embedding worker stopped.");
    }
}
