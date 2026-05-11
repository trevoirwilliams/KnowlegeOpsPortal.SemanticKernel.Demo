using System;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Web.Models.Copilot;

public sealed class CopilotHistoryOptions
{
    public const string SectionName = "CopilotHistory";

    [Range(2, 50)]
    public int MaxModelMessages { get; init; } = 8;

    [Range(2, 200)]
    public int MaxDisplayMessages { get; init; } = 50;

    [Range(4, 100)]
    public int SummarizeAfterMessages { get; init; } = 16;

    [Range(2, 50)]
    public int MessagesToKeepAfterSummary { get; init; } = 8;

    [Range(1, 168)]
    public int ConversationRelevanceHours { get; init; } = 24;

    [Range(1, 365)]
    public int RetentionDays { get; init; } = 10;

    [Range(1, 30)]
    public int CleanupIntervalDays { get; init; } = 10;
}