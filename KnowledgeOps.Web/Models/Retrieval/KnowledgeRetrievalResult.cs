namespace KnowledgeOps.Web.Models.Retrieval;

public sealed class KnowledgeRetrievalResult
{
    public required string Question { get; init; }

    public IReadOnlyList<RetrievedKnowledgeChunk> Chunks { get; init; } = [];

    public bool HasRelevantContent => Chunks.Count > 0;
}