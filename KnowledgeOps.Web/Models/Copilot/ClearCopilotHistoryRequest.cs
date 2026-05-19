namespace KnowledgeOps.Web.Models.Copilot;

public sealed class ClearCopilotHistoryRequest
{
    public int? ConversationId { get; init; }

    public CopilotPageContext? Context { get; init; }
}