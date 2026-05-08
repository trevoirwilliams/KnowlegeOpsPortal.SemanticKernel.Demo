using System;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotPageContext
{
    public string? Area { get; init; }

    public string? PageTitle { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? Summary { get; init; }

    public Dictionary<string, string> Metadata { get; init; } = [];
}
