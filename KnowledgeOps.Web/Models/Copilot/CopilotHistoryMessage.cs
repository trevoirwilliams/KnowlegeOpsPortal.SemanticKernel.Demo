namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotHistoryMessage
{
    public required string Role { get; init; }

    public required string Content { get; init; }

    public required DateTime CreatedUtc { get; init; }
}
