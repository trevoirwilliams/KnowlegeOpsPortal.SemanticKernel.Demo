namespace KnowledgeOps.Web.Models.Retrieval;

public sealed class KnowledgeRetrievalRequest
{
    public required string Question { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? UserId { get; init; }

    public int MaxResults { get; init; } = 5;
}
