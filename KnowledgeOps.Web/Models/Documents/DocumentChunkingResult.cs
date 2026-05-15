using System;

namespace KnowledgeOps.Web.Models.Documents;

public sealed class DocumentChunkingResult
{
    public int ChunkCount { get; init; }

    public int TotalCharacters { get; init; }

    public int EstimatedTokenCount { get; init; }
}
