using System;
using Microsoft.Extensions.VectorData;

namespace KnowledgeOps.Web.Models.VectorStores;

public sealed class SearchableDocumentChunk
{
    [VectorStoreKey]
    public string Key { get; init; } = string.Empty;

    [VectorStoreData]
    public string PortalDocumentId { get; init; } = string.Empty;

    [VectorStoreData]
    public string PortalDocumentChunkId { get; init; } = string.Empty;

    [VectorStoreData]
    public int ChunkIndex { get; init; }

    [VectorStoreData]
    public string SourceFileName { get; init; } = string.Empty;

    [VectorStoreData]
    public string? DocumentTags { get; init; }

    [VectorStoreData]
    public string Text { get; init; } = string.Empty;

    [VectorStoreVector(
        Dimensions: 1536,
        DistanceFunction = DistanceFunction.CosineDistance)]
    public string EmbeddingText => Text;
}
