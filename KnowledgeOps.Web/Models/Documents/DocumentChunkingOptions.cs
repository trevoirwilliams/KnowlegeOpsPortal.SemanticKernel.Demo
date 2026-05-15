using System;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Web.Models.Documents;

public sealed class DocumentChunkingOptions
{
    public const string SectionName = "DocumentChunking";

    [Range(20, 2_000)]
    public int MaxTokensPerLine { get; init; } = 120;

    [Range(100, 8_000)]
    public int MaxTokensPerChunk { get; init; } = 500;

    [Range(0, 1_000)]
    public int OverlapTokens { get; init; } = 80;
}
