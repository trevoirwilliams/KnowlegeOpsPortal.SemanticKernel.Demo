using KnowledgeOps.Domain.Models.Enums;

namespace KnowledgeOps.Web.Models.Documents;

public sealed class DocumentListItemViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusCssClass { get; init; } = "text-bg-secondary";
    
    public DateOnly LastReviewedOn { get; init; }

    public string Summary { get; init; } = string.Empty;

    public string SourceLabel { get; init; } = "Knowledge Base";

    public string DetailsAction { get; init; } = "Details";

}

