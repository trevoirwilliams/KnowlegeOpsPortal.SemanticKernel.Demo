using System;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Domain.Models;

public class PortalDocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PortalDocumentId { get; set; }

    public PortalDocument PortalDocument { get; set; } = null!;

    public int ChunkIndex { get; set; }

    public int CharacterStart { get; set; }

    public int CharacterEnd { get; set; }

    public int CharacterCount { get; set; }

    public int EstimatedTokenCount { get; set; }

    public string Text { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    [MaxLength(255)]
    public string SourceFileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? DocumentTags { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
