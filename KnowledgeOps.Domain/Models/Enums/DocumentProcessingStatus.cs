namespace KnowledgeOps.Domain.Models.Enums;

public enum DocumentProcessingStatus
{
    Uploaded = 1,
    TextExtracted = 2,
    RequiresOcr = 3,
    OcrCompleted = 4,
    Preprocessed = 5,
    Chunked = 6,
    Embedded = 7,
    Ready = 8,
    Failed = 9
}

