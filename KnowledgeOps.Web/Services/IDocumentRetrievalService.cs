using System;
using KnowledgeOps.AI;
using KnowledgeOps.Web.Models.Retrieval;
using KnowledgeOps.Web.Models.VectorStores;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;

namespace KnowledgeOps.Web.Services;

public interface IDocumentRetrievalService
{
    Task<KnowledgeRetrievalResult> RetrieveRelevantKnowledgeAsync(
        KnowledgeRetrievalRequest request,
        CancellationToken cancellationToken = default);
}

public class DocumentRetrievalService(
    VectorStore vectorStore,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IOptions<AzureOpenAIOptions> azureOpenAIOptions,
    ILogger<DocumentRetrievalService> logger) : IDocumentRetrievalService
{
    private readonly AzureOpenAIOptions _azureOpenAIOptions = azureOpenAIOptions.Value;

    public async Task<KnowledgeRetrievalResult> RetrieveRelevantKnowledgeAsync(KnowledgeRetrievalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new ArgumentException(
                "A question is required before knowledge retrieval can run.",
                nameof(request));
        }

        int maxResults = Math.Clamp(request.MaxResults, 1, 10);
        string question = request.Question.Trim();

        VectorStoreCollectionDefinition collectionDefinition =
            DocumentChunkVectorCollection.CreateDefinition(
                embeddingGenerator,
                _azureOpenAIOptions);

        VectorStoreCollection<string, SearchableDocumentChunk> collection =
            vectorStore.GetCollection<string, SearchableDocumentChunk>(
                DocumentChunkVectorCollection.CollectionName,
                collectionDefinition);

        await collection.EnsureCollectionExistsAsync(cancellationToken);

        VectorSearchOptions<SearchableDocumentChunk> searchOptions =
            BuildSearchOptions(request);

        List<RetrievedKnowledgeChunk> chunks = [];

        await foreach (var result
            in collection.SearchAsync(
                question,
                maxResults,
                searchOptions,
                cancellationToken))
        {
            chunks.Add(new RetrievedKnowledgeChunk
            {
                ChunkId = result.Record.PortalDocumentChunkId,
                DocumentId = result.Record.PortalDocumentId,
                SourceFileName = result.Record.SourceFileName,
                ChunkIndex = result.Record.ChunkIndex,
                DocumentTags = result.Record.DocumentTags,
                Text = result.Record.Text,
                Score = result.Score
            });
        }

        logger.LogInformation(
            "Retrieved {ChunkCount} knowledge chunks. EntityType: {EntityType}, EntityId: {EntityId}",
            chunks.Count,
            request.EntityType,
            request.EntityId);

        return new KnowledgeRetrievalResult
        {
            Question = question,
            Chunks = chunks
        };
    }

    private VectorSearchOptions<SearchableDocumentChunk> BuildSearchOptions(KnowledgeRetrievalRequest request)
    {
        var options = new VectorSearchOptions<SearchableDocumentChunk>
        {
            IncludeVectors = false
        };

        if (IsDocumentContext(request, out Guid documentId))
        {
            string normalizedDocumentId = documentId.ToString("N");

            options.Filter = chunk =>
                chunk.PortalDocumentId == normalizedDocumentId;
        }

        return options;
    }

    private bool IsDocumentContext(KnowledgeRetrievalRequest request, out Guid documentId)
    {
        documentId = Guid.Empty;

        if (!string.Equals(
                request.EntityType,
                "Document",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Guid.TryParse(request.EntityId, out documentId);
    }
}
