using System;
using System.Security.Cryptography;
using System.Text;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using KnowledgeOps.Web.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Text;

namespace KnowledgeOps.Web.Services;

public interface IDocumentChunkingService
{
    DocumentChunkingResult PrepareChunks(
        PortalDocument document,
        string normalizedText,
        out IReadOnlyList<PortalDocumentChunk> chunks);

    Task<bool> ChunkNextTextExtractedDocumentAsync(
        CancellationToken cancellationToken = default);

    Task ChunkClaimedDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}

public class DocumentChunkingService(
    ApplicationDbContext dbContext,
    IOptions<DocumentChunkingOptions> options,
    ILogger<DocumentChunkingService> logger) : IDocumentChunkingService
{
    private readonly DocumentChunkingOptions _options = options.Value;

    public DocumentChunkingResult PrepareChunks(PortalDocument document, string normalizedText, out IReadOnlyList<PortalDocumentChunk> preparedChunks)
    {
       if (string.IsNullOrWhiteSpace(normalizedText))
        {
            throw new InvalidOperationException(
                "The document does not contain text that can be chunked.");
        }

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        List<string> lines = TextChunker.SplitPlainTextLines(
            normalizedText,
            maxTokensPerLine: _options.MaxTokensPerLine);

        List<string> chunks = TextChunker.SplitPlainTextParagraphs(
            lines,
            maxTokensPerParagraph: _options.MaxTokensPerChunk,
            overlapTokens: _options.OverlapTokens);
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                "Semantic Kernel could not create useful chunks from the document text.");
        }

        document.Chunks.Clear();
        for (int index = 0; index < chunks.Count; index++)
        {
            string chunkText = chunks[index].Trim();

            if (string.IsNullOrWhiteSpace(chunkText))
            {
                continue;
            }

            document.Chunks.Add(new PortalDocumentChunk
            {
                PortalDocumentId = document.Id,
                ChunkIndex = index,
                Text = chunkText,
                CharacterCount = chunkText.Length,
                EstimatedTokenCount = EstimateTokenCount(chunkText),
                ContentHash = ComputeSha256Hash(chunkText),
                SourceFileName = document.OriginalFileName,
                DocumentTags = document.Tags,
                CreatedUtc = DateTime.UtcNow
            });
        }

        int chunkCount = document.Chunks.Count;

        if (chunkCount == 0)
        {
            throw new InvalidOperationException(
                "No non-empty chunks were created from the document text.");
        }

        int estimatedTokenCount = document.Chunks.Sum(
            chunk => chunk.EstimatedTokenCount);

        logger.LogInformation(
            "Prepared {ChunkCount} chunks for document {DocumentId}. Estimated tokens: {EstimatedTokenCount}.",
            chunkCount,
            document.Id,
            estimatedTokenCount);

        preparedChunks = document.Chunks.ToList().AsReadOnly();
        return new DocumentChunkingResult
        {
            ChunkCount = chunkCount,
            TotalCharacters = normalizedText.Length,
            EstimatedTokenCount = estimatedTokenCount
        };
    }
    public async Task ChunkClaimedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        PortalDocument? document = await dbContext.PortalDocuments
            .Include(document => document.Content)
            .FirstOrDefaultAsync(
                document => document.Id == documentId,
                cancellationToken);

        if (document is null)
        {
            logger.LogWarning(
                "Document {DocumentId} was not found for chunking.",
                documentId);

            return;
        }

        if (document.ProcessingStatus != DocumentProcessingStatus.Preprocessed)
        {
            logger.LogInformation(
                "Document {DocumentId} is not in Preprocessed state. Current state: {Status}",
                document.Id,
                document.ProcessingStatus);

            return;
        }

        if (document.Content is null ||
            string.IsNullOrWhiteSpace(document.Content.RawText))
        {
            await MarkFailedAsync(
                document.Id,
                "The document does not have extracted text to chunk.",
                cancellationToken);

            return;
        }

        try
        {
            DocumentChunkingResult result =
                PrepareChunks(
                    document,
                    document.Content.RawText,
                    out IReadOnlyList<PortalDocumentChunk> chunks);

            await dbContext.PortalDocumentChunks
                .Where(chunk => chunk.PortalDocumentId == document.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.PortalDocumentChunks.AddRangeAsync(
                chunks,
                cancellationToken);

            document.ProcessingStatus = DocumentProcessingStatus.Chunked;
            document.ProcessingError = null;
            document.ProcessedUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Document {DocumentId} was chunked successfully. Chunks: {ChunkCount}. Estimated tokens: {EstimatedTokenCount}.",
                document.Id,
                result.ChunkCount,
                result.EstimatedTokenCount);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while chunking document {DocumentId}.",
                document.Id);

            await MarkFailedAsync(
                document.Id,
                "An error occurred during document chunking. Please try uploading the document again.",
                cancellationToken);
        }
    }

    public async Task<bool> ChunkNextTextExtractedDocumentAsync(CancellationToken cancellationToken = default)
    {
        Guid? documentId = await dbContext.PortalDocuments
            .AsNoTracking()
            .Where(document =>
                document.ProcessingStatus == DocumentProcessingStatus.TextExtracted)
            .OrderBy(document => document.ProcessedUtc ?? document.UploadedUtc)
            .Select(document => (Guid?)document.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentId is null)
        {
            return false;
        }

        int claimedRows = await dbContext.PortalDocuments
            .Where(d =>
                d.Id == documentId.Value &&
                d.ProcessingStatus == DocumentProcessingStatus.TextExtracted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.ProcessingStatus, DocumentProcessingStatus.Preprocessed)
                .SetProperty(d => d.ProcessingError, (string?)null),
                cancellationToken);

        if (claimedRows == 0)
        {
            logger.LogInformation(
                "Document {DocumentId} was already claimed by another worker.",
                documentId.Value);

            return false;
        }

        await ChunkClaimedDocumentAsync(documentId.Value, cancellationToken);
        return true;
    }
    
    private string ComputeSha256Hash(string chunkText)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(chunkText);
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private int EstimateTokenCount(string chunkText)
    {
        return Math.Max(1, (int)Math.Ceiling(chunkText.Length / 4.0));
    }

     private async Task MarkFailedAsync(
        Guid documentId,
        string message,
        CancellationToken cancellationToken)
    {
        await dbContext.PortalDocuments
            .Where(document => document.Id == documentId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        document => document.ProcessingStatus,
                        DocumentProcessingStatus.Failed)
                    .SetProperty(
                        document => document.ProcessingError,
                        message)
                    .SetProperty(
                        document => document.ProcessedUtc,
                        DateTime.UtcNow),
                cancellationToken);
    }
}
