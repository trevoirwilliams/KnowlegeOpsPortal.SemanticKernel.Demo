using System;
using KnowledgeOps.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace KnowledgeOps.Web.Models.VectorStores;

public static class DocumentChunkVectorCollection
{
    public const string CollectionName = "portal_document_chunks";
    public static VectorStoreCollectionDefinition CreateDefinition(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        AzureOpenAIOptions azureOpenAIOptions)
    {
        return new VectorStoreCollectionDefinition
        {
            EmbeddingGenerator = embeddingGenerator,
            Properties =
            [
                new VectorStoreKeyProperty(
                    nameof(SearchableDocumentChunk.Key),
                    typeof(string)),

                new VectorStoreDataProperty(
                    nameof(SearchableDocumentChunk.PortalDocumentId),
                    typeof(string)),

                new VectorStoreDataProperty(
                    nameof(SearchableDocumentChunk.PortalDocumentChunkId),
                    typeof(string)),

                new VectorStoreDataProperty(
                    nameof(SearchableDocumentChunk.ChunkIndex),
                    typeof(int)),

                new VectorStoreDataProperty(
                    nameof(SearchableDocumentChunk.SourceFileName),
                    typeof(string)),

                new VectorStoreDataProperty(
                    nameof(SearchableDocumentChunk.DocumentTags),
                    typeof(string)),

                new VectorStoreDataProperty(
                    nameof(SearchableDocumentChunk.Text),
                    typeof(string)),

                new VectorStoreVectorProperty(
                    nameof(SearchableDocumentChunk.EmbeddingText),
                    typeof(string),
                    dimensions: azureOpenAIOptions.EmbeddingDimensions)
                {
                    DistanceFunction = DistanceFunction.CosineDistance
                }
            ]
        };
    }
}