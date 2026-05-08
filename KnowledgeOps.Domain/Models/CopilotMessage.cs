namespace KnowledgeOps.Domain.Models;

public class CopilotMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public CopilotConversation Conversation { get; set; } = default!;

    public CopilotMessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public int SequenceNumber { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

