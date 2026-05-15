using KnowledgeOps.Domain.Models.Enums;

namespace KnowledgeOps.Web.Models.Documents;

public sealed class DocumentDetailsViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public string Owner { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusCssClass { get; init; } = "text-bg-secondary";

    public DateOnly LastReviewedOn { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string SourceLabel { get; init; } = "Knowledge Base";

    public string? ContentType { get; init; }

    public long? FileSizeBytes { get; init; }

    public string? ProcessingError { get; init; }

    public string? ExtractedTextPreview { get; init; }

    public int? CharacterCount { get; init; }

    public int? PageCount { get; init; }

    public string? ExtractionEngine { get; init; }

    public int ChunkCount { get; init; }

    public IReadOnlyList<DocumentChunkPreviewViewModel> ChunkPreviews { get; init; } = [];
}
