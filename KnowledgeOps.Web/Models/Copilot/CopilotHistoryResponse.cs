using System;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotHistoryResponse
{
    public int? ConversationId { get; init; }

    public string? ContextSummary { get; init; }

    public IReadOnlyList<CopilotHistoryMessage> Messages { get; init; } = [];
}
