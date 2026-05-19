using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using KnowledgeOps.Web.Models.Documents;
using KnowledgeOps.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Web.Controllers;

public class DocumentsController(
    IDocumentUploadService documentUploadService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await documentUploadService.GetUploadedDocumentsAsync(cancellationToken);

        var model = documents
            .Select(MapToListItem)
            .ToList();

        return View(model);
    }

    public async Task<IActionResult> Details(string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out Guid documentId))
        {
            return BadRequest();
        }

        var document = await documentUploadService.GetUploadedDocumentAsync(
            documentId,
            cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        var model = MapToDetails(document);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        UploadDocumentViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            TempData["ErrorMessage"] = "Please select a PDF document before uploading.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await documentUploadService.UploadAsync(
                model.File,
                model.Tags,
                cancellationToken);

            TempData["SuccessMessage"] = "Document uploaded successfully.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An unexpected error occurred while uploading the document. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static DocumentDetailsViewModel MapToDetails(
        PortalDocument document)
    {
        return new DocumentDetailsViewModel
        {
            Id = document.Id.ToString(),
            Title = document.OriginalFileName,
            Category = "Uploaded PDF",
            Department = "User Upload",
            Owner = "Current User",
            StatusLabel = GetProcessingStatusLabel(document.ProcessingStatus),
            StatusCssClass = GetProcessingStatusCssClass(document.ProcessingStatus),
            LastReviewedOn = DateOnly.FromDateTime(document.UploadedUtc),
            Summary = document.ExtractedTextPreview
                ?? "This document has been uploaded and is waiting for text extraction.",
            Tags = SplitTags(document.Tags),
            SourceLabel = "Uploaded Document",
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            ProcessingError = document.ProcessingError,
            ExtractedTextPreview = document.ExtractedTextPreview
        };
    }

    private static IReadOnlyList<string> SplitTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        return tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private static DocumentListItemViewModel MapToListItem(
        PortalDocument document)
    {
        return new DocumentListItemViewModel
        {
            Id = document.Id.ToString(),
            Title = document.OriginalFileName,
            Category = "Uploaded PDF",
            Department = "User Upload",
            StatusLabel = GetProcessingStatusLabel(document.ProcessingStatus),
            StatusCssClass = GetProcessingStatusCssClass(document.ProcessingStatus),
            LastReviewedOn = DateOnly.FromDateTime(document.UploadedUtc),
            Summary = document.ExtractedTextPreview
                ?? "This document has been uploaded and is waiting for text extraction.",
            SourceLabel = "Uploaded Document",
            DetailsAction = nameof(Details)
        };
    }

    private static string GetProcessingStatusCssClass(
        DocumentProcessingStatus status)
    {
        return status switch
        {
            DocumentProcessingStatus.Uploaded => "text-bg-primary",
            DocumentProcessingStatus.TextExtracted => "text-bg-info",
            DocumentProcessingStatus.RequiresOcr => "text-bg-warning",
            DocumentProcessingStatus.OcrCompleted => "text-bg-info",
            DocumentProcessingStatus.Preprocessed => "text-bg-secondary",
            DocumentProcessingStatus.Chunked => "text-bg-secondary",
            DocumentProcessingStatus.Embedded => "text-bg-secondary",
            DocumentProcessingStatus.Ready => "text-bg-success",
            DocumentProcessingStatus.Failed => "text-bg-danger",
            _ => "text-bg-light"
        };
    }

    private static string GetProcessingStatusLabel(
        DocumentProcessingStatus status)
    {
        return status switch
        {
            DocumentProcessingStatus.RequiresOcr => "Requires OCR",
            DocumentProcessingStatus.OcrCompleted => "OCR Completed",
            DocumentProcessingStatus.TextExtracted => "Text Extracted",
            _ => status.ToString()
        };
    }
}
