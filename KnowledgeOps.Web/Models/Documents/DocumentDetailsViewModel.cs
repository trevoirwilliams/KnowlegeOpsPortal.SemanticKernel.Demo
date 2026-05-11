using KnowledgeOps.Domain.Models.Enums;

namespace KnowledgeOps.Web.Models.Documents;

public sealed class DocumentDetailsViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public string Owner { get; init; } = string.Empty;

    public KnowledgeDocumentStatus Status { get; init; }

    public DateOnly LastReviewedOn { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = [];
}
