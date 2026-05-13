using System;
using IronOcr;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Web.Services;

public interface IDocumentProcessingService
{
    Task<bool> ProcessNextQueuedDocumentAsync(
        CancellationToken cancellationToken = default);

    Task ProcessClaimedDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}

public class DocumentProcessingService(
    ApplicationDbContext dbContext,
    ILogger<DocumentProcessingService> logger) : IDocumentProcessingService
{
    private const int PreviewLength = 1_000;
    private const int MinimumUsefulTextLength = 50;

    public async Task ProcessClaimedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.PortalDocuments
            .Include(d => d.Content)
            .FirstOrDefaultAsync(
                d => d.Id == documentId,
                cancellationToken);

        if (document is null)
        {
            logger.LogWarning(
                "Document {DocumentId} was not found for text extraction.",
                documentId);

            return;
        }

        if (document.ProcessingStatus != DocumentProcessingStatus.ExtractingText)
        {
            logger.LogInformation(
                "Document {DocumentId} is not in ExtractingText state. Current state: {Status}",
                document.Id,
                document.ProcessingStatus);

            return;
        }

        if (!File.Exists(document.StoredFilePath))
        {
            await MarkFailedAsync(
                document.Id,
                "The stored PDF file could not be found.",
                cancellationToken);

            return;
        }

        try
        {
            using PdfDocument pdf = PdfDocument.FromFile(document.StoredFilePath);

            string rawText = NormalizeExtractedText(pdf.ExtractAllText());
            int pageCount = pdf.PageCount;

            if (string.IsNullOrWhiteSpace(rawText) ||
                rawText.Length < MinimumUsefulTextLength)
            {
                logger.LogInformation(
                    "No useful embedded text found for document {DocumentId}. Running OCR fallback.",
                    document.Id);
                rawText = ExtractTextWithOcrAsync(
                    document.StoredFilePath);
                    
                rawText = NormalizeExtractedText(rawText);

                if (string.IsNullOrWhiteSpace(rawText) ||
                    rawText.Length < MinimumUsefulTextLength)
                {
                    logger.LogInformation(
                        "No useful text found for document {DocumentId} after OCR fallback.",
                        document.Id);
                    await MarkFailedAsync(
                        document.Id,
                        "No useful text could be extracted from the document.",
                        cancellationToken);
                    return;
                }
            }

            if (document.Content is null)
            {
                dbContext.PortalDocumentContents.Add(new PortalDocumentContent
                {
                    PortalDocumentId = document.Id,
                    RawText = rawText,
                    CharacterCount = rawText.Length,
                    PageCount = pageCount,
                    ExtractionEngine = "IronPDF",
                    ExtractedUtc = DateTime.UtcNow
                });
            }
            else
            {
                document.Content.RawText = rawText;
                document.Content.CharacterCount = rawText.Length;
                document.Content.PageCount = pageCount;
                document.Content.ExtractionEngine = "IronPDF";
                document.Content.ExtractedUtc = DateTime.UtcNow;
            }

            document.ExtractedTextPreview = CreatePreview(rawText);
            document.ProcessingStatus = DocumentProcessingStatus.TextExtracted;
            document.ProcessingError = null;
            document.ProcessedUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing document {DocumentId}.", document.Id);

            await MarkFailedAsync(
                document.Id,
                "An error occurred during document processing. Please try again.",
                cancellationToken);
        }
    }

    private string ExtractTextWithOcrAsync(string storedFilePath)
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadPdf(storedFilePath);
        OcrResult result = ocr.Read(input);
        return result.Text;
    }

    public async Task<bool> ProcessNextQueuedDocumentAsync(CancellationToken cancellationToken = default)
    {
        Guid? documentId = await dbContext.PortalDocuments
            .AsNoTracking()
            .Where(d => d.ProcessingStatus == DocumentProcessingStatus.Uploaded)
            .OrderBy(d => d.UploadedUtc)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentId == null)
        {
            return false;
        }

        int claimedRows = await dbContext.PortalDocuments
            .Where(d =>
                d.Id == documentId.Value &&
                d.ProcessingStatus == DocumentProcessingStatus.Uploaded)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.ProcessingStatus,   DocumentProcessingStatus.ExtractingText)
                .SetProperty(d => d.ProcessingError,(string?)null),
                cancellationToken);

        if (claimedRows == 0)
        {
            logger.LogInformation(
                "Document {DocumentId} was already claimed by another worker.",
                documentId.Value);

            return false;
        }

        await ProcessClaimedDocumentAsync(documentId.Value, cancellationToken);
        return true;
    }

    private async Task MarkFailedAsync(Guid documentId, string message, CancellationToken cancellationToken)
    {
        await dbContext.PortalDocuments
            .Where(d => d.Id == documentId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(d => d.ProcessingStatus, DocumentProcessingStatus.Failed)
                    .SetProperty(d => d.ProcessingError, message)
                    .SetProperty(d => d.ProcessedUtc, DateTime.UtcNow),
                cancellationToken);
    }

    private string? CreatePreview(string rawText)
    {
        if (rawText.Length <= PreviewLength)
        {
            return rawText;
        }

        return rawText[..PreviewLength] + "...";
    }

    private string NormalizeExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
    }

}