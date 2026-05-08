using System;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotPanelViewModel
{
    public string Title { get; init; } = "KnowledgeOps Assistant";

    public string Subtitle { get; init; } = "Ask questions about documents, requests, and portal workflows.";

    public string Placeholder { get; init; } = "Ask the assistant for help...";

    public IReadOnlyList<string> SuggestedPrompts { get; init; } =
    [
        "What can you help me with?",
        "Summarize the current page.",
        "What should I review next?"
    ];
}
