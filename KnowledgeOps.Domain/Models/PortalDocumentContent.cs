using System;
using System.ComponentModel.DataAnnotations;
namespace KnowledgeOps.Domain.Models;

public class PortalDocumentContent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PortalDocumentId { get; set; }

    public PortalDocument PortalDocument { get; set; } = null!;

    public string RawText { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int PageCount { get; set; }

    [MaxLength(100)]
    public string ExtractionEngine { get; set; } = "IronPDF";

    public DateTime ExtractedUtc { get; set; } = DateTime.UtcNow;
}
