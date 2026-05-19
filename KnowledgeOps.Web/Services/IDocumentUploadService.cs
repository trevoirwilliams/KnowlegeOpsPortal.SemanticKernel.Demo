using System;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using KnowledgeOps.Web.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Web.Services;

public interface IDocumentUploadService
{
    Task<IReadOnlyList<PortalDocument>> GetUploadedDocumentsAsync(
        CancellationToken cancellationToken = default);

    Task<PortalDocument?> GetUploadedDocumentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PortalDocument> UploadAsync(
        IFormFile file,
        string? tags = null,
        CancellationToken cancellationToken = default);

}

public class DocumentUploadService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<DocumentUploadOptions> options) : IDocumentUploadService
{
    private readonly DocumentUploadOptions _options = options.Value;

    public async Task<PortalDocument?> GetUploadedDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string? userId = currentUserService.UserId;
        return await dbContext.PortalDocuments
            .AsNoTracking()
            .Include(document => document.Content)
            .Include(document => document.Chunks)
            .FirstOrDefaultAsync(
                document => document.Id == id && document.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PortalDocument>> GetUploadedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        string? userId = currentUserService.UserId;

        return await dbContext.PortalDocuments
            .AsNoTracking()
            .Where(document => document.UserId == userId)
            .OrderByDescending(document => document.UploadedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PortalDocument> UploadAsync(IFormFile file, string? tags = null, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        string? userId = currentUserService.UserId;

        string uploadsFolder = Path.Combine(webHostEnvironment.ContentRootPath, _options.StoragePath);
        Directory.CreateDirectory(uploadsFolder);

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using FileStream fileStream = File.Create(filePath);
        await file.CopyToAsync(fileStream, cancellationToken);

        var document = new PortalDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalFileName = file.FileName,
            StoredFilePath = filePath,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedUtc = DateTime.UtcNow,
            Tags = NormalizeTags(tags),
            ProcessingStatus = DocumentProcessingStatus.Uploaded
        };

        dbContext.PortalDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        return document;
    }

    private string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        var normalizedTags = tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.ToLowerInvariant())
            .Distinct()
            .ToList();

        return normalizedTags.Count == 0
            ? null
            : string.Join(", ", normalizedTags);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("The selected file is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"The selected file exceeds the maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"The selected file type '{extension}' is not allowed.");
        }

        string contentType = file.ContentType.ToLowerInvariant();
        if (!_options.AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"The selected file content type '{contentType}' is not allowed.");
        }
    }
}