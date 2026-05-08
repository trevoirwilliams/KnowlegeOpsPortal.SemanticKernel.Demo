namespace KnowledgeOps.Domain.Models;

public class CopilotConversation
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public CopilotContextType ContextType { get; set; } = CopilotContextType.General;

    public string? ContextId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CopilotMessage> Messages { get; set; } = [];
}

