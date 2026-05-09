using System;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class PersistedConversationTurn
{
    public required int ConversationId { get; init; }

    public required int UserMessageId { get; init; }

    public required int AssistantMessageId { get; init; }
}
