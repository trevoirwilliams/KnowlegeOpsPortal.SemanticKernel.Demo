namespace KnowledgeOps.Web.Models.Documents;

public sealed class DocumentChunkPreviewViewModel
{
    public int ChunkIndex { get; init; }

    public int CharacterCount { get; init; }

    public int EstimatedTokenCount { get; init; }

    public string Preview { get; init; } = string.Empty;
}