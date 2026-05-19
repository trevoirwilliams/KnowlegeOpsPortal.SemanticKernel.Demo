using System;
using KnowledgeOps.AI.Services;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Web.Models.Copilot;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KnowledgeOps.Web.Services;

public interface ICopilotHistorySummarizerService
{
    Task SummarizeIfNeededAsync(
        CopilotConversation conversation,
        CancellationToken cancellationToken = default);
}

public sealed class CopilotHistorySummarizerService(
    ApplicationDbContext dbContext,
    IKnowledgeOpsChatClient chatClient,
    IOptions<CopilotHistoryOptions> options,
    ILogger<CopilotHistorySummarizerService> logger) : ICopilotHistorySummarizerService
{
    public async Task SummarizeIfNeededAsync(
        CopilotConversation conversation,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        var totalMessages = await dbContext.CopilotMessages
            .CountAsync(
                message => message.ConversationId == conversation.Id,
                cancellationToken);

        if (totalMessages < settings.SummarizeAfterMessages)
        {
            return;
        }

        var lastSummarizedSequence = conversation.SummarizedThroughSequenceNumber ?? 0;

        var latestSequence = await dbContext.CopilotMessages
            .Where(message => message.ConversationId == conversation.Id)
            .Select(message => (int?)message.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var summarizeThroughSequence = latestSequence - settings.MessagesToKeepAfterSummary;

        if (summarizeThroughSequence <= lastSummarizedSequence)
        {
            return;
        }

        var messagesToSummarize = await dbContext.CopilotMessages
            .Where(message =>
                message.ConversationId == conversation.Id &&
                message.SequenceNumber > lastSummarizedSequence &&
                message.SequenceNumber <= summarizeThroughSequence)
            .OrderBy(message => message.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (messagesToSummarize.Count == 0)
        {
            return;
        }

        var conversationText = BuildConversationText(messagesToSummarize);

        var updatedSummary = await chatClient.SummarizeConversationHistoryAsync(
            conversation.Summary ?? string.Empty,
            conversationText,
            cancellationToken);

        conversation.Summary = updatedSummary;
        conversation.SummarizedUtc = DateTime.UtcNow;
        conversation.SummarizedThroughSequenceNumber = summarizeThroughSequence;
        conversation.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Summarized copilot conversation {ConversationId} through sequence {SequenceNumber}.",
            conversation.Id,
            summarizeThroughSequence);
    }

    private static string BuildConversationText(IReadOnlyList<CopilotMessage> messages)
    {
        var builder = new StringBuilder();

        foreach (var message in messages)
        {
            builder.AppendLine($"{message.Role}: {message.Content}");
            builder.AppendLine();
        }

        return builder.ToString();
    }
}