using System;

namespace KnowledgeOps.Web.Models.Retrieval;

public sealed class RetrievedKnowledgeChunk
{
    public required string ChunkId { get; init; }

    public required string DocumentId { get; init; }

    public required string SourceFileName { get; init; }

    public int ChunkIndex { get; init; }

    public string? DocumentTags { get; init; }

    public required string Text { get; init; }

    public double? Score { get; init; }
}
