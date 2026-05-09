using System;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotRequest
{
    [Required]
    [StringLength(2_000, MinimumLength = 2)]
    public string Message { get; init; } = string.Empty;

    public CopilotPageContext? Context { get; init; }

    public int? ConversationId { get; init; }
}
