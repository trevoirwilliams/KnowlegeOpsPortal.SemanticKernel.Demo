using System;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotSourceReference
{
    public required string SourceFileName { get; init; }

    public required string DocumentId { get; init; }

    public required string ChunkId { get; init; }

    public int ChunkIndex { get; init; }

    public string? DocumentTags { get; init; }

    public double? Score { get; init; }

    public string PreviewText { get; init; } = string.Empty;
}
