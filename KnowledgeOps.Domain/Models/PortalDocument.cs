using System.ComponentModel.DataAnnotations;
using KnowledgeOps.Domain.Models.Enums;

namespace KnowledgeOps.Domain.Models;

public class PortalDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? UserId { get; set; }

    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string StoredFilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = "application/pdf";

    public long FileSizeBytes { get; set; }

    public DocumentProcessingStatus ProcessingStatus { get; set; } = DocumentProcessingStatus.Uploaded;

    public string? ExtractedTextPreview { get; set; }

    public string? ProcessingError { get; set; }

    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedUtc { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public PortalDocumentContent? Content { get; set; }

    public ICollection<PortalDocumentChunk> Chunks { get; set; } = [];
}

