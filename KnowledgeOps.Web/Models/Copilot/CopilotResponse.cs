using System;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotResponse
{
    public int? ConversationId { get; init; }
    
    public string Message { get; init; } = string.Empty;

    public string? ContextSummary { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
