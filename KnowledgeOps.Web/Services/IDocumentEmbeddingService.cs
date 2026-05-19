using System;
using KnowledgeOps.AI;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using KnowledgeOps.Web.Models.VectorStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;

namespace KnowledgeOps.Web.Services;

public interface IDocumentEmbeddingService
{
    Task<bool> EmbedNextChunkedDocumentAsync(
        CancellationToken cancellationToken = default);

    Task EmbedDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}

public class DocumentEmbeddingService(
    ApplicationDbContext dbContext,
    VectorStore vectorStore,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IOptions<AzureOpenAIOptions> azureOpenAIOptions,
    ILogger<DocumentEmbeddingService> logger) : IDocumentEmbeddingService
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions = azureOpenAIOptions.Value;

    public async Task EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        PortalDocument? document = await dbContext.PortalDocuments
            .Include(document => document.Chunks)
            .FirstOrDefaultAsync(
                document => document.Id == documentId,
                cancellationToken);

        if (document is null)
        {
            logger.LogWarning(
                "Document {DocumentId} was not found for embedding.",
                documentId);

            return;
        }

        if (document.Chunks.Count == 0)
        {
            await MarkFailedAsync(
                document.Id,
                "The document does not have chunks to embed.",
                cancellationToken);

            return;
        }

        try
        {
            VectorStoreCollectionDefinition collectionDefinition =
                DocumentChunkVectorCollection.CreateDefinition(
                    embeddingGenerator,
                    _azureOpenAIOptions);

            VectorStoreCollection<string, SearchableDocumentChunk> collection =
                vectorStore.GetCollection<string, SearchableDocumentChunk>(
                    DocumentChunkVectorCollection.CollectionName, collectionDefinition);

            await collection.EnsureCollectionExistsAsync(cancellationToken);
            SearchableDocumentChunk[] records = document.Chunks
                .OrderBy(chunk => chunk.ChunkIndex)
                .Select(chunk => new SearchableDocumentChunk
                {
                    Key = chunk.Id.ToString("N"),
                    PortalDocumentId = chunk.PortalDocumentId.ToString("N"),
                    PortalDocumentChunkId = chunk.Id.ToString("N"),
                    ChunkIndex = chunk.ChunkIndex,
                    SourceFileName = chunk.SourceFileName,
                    DocumentTags = chunk.DocumentTags,
                    Text = chunk.Text
                })
                .ToArray();
            await collection.UpsertAsync(records, cancellationToken);

            document.ProcessingStatus = DocumentProcessingStatus.Ready;
            document.ProcessingError = null;
            document.ProcessedUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Document {DocumentId} was embedded and stored as searchable knowledge. Chunks: {ChunkCount}.",
                document.Id,
                records.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while embedding document {DocumentId}.", documentId);
            await MarkFailedAsync(
                document.Id,
                "An error occurred during embedding. See logs for details.",
                cancellationToken);
        }
    }

    

    public async Task<bool> EmbedNextChunkedDocumentAsync(CancellationToken cancellationToken = default)
    {
        Guid? documentId = await dbContext.PortalDocuments
            .AsNoTracking()
            .Where(document => document.ProcessingStatus == DocumentProcessingStatus.Chunked)
            .OrderBy(document => document.ProcessedUtc ?? document.UploadedUtc)
            .Select(document => (Guid?)document.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentId is null)
        {
            return false;
        }

        int claimedRows = await dbContext.PortalDocuments
            .Where(document =>
                document.Id == documentId.Value &&
                document.ProcessingStatus == DocumentProcessingStatus.Chunked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.ProcessingStatus, DocumentProcessingStatus.Embedded)
                .SetProperty(document => document.ProcessingError, (string?)null),
                cancellationToken);

        if (claimedRows == 0)
        {
            logger.LogInformation(
                "Document {DocumentId} was already claimed by another embedding worker.",
                documentId.Value);

            return false;
        }

        await EmbedDocumentAsync(documentId.Value, cancellationToken);
        return true;
    }

    private async Task MarkFailedAsync(Guid documentId, string errorMessage, CancellationToken cancellationToken)
    {
        PortalDocument? document = await dbContext.PortalDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is not null)
        {
            document.ProcessingStatus = DocumentProcessingStatus.Failed;
            document.ProcessingError = errorMessage;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
