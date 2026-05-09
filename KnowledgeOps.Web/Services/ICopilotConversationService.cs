using System;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Web.Models.Copilot;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Web.Services;

public interface ICopilotConversationService
{
    Task<CopilotConversation> GetOrCreateConversationAsync(
        CopilotRequest request,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<int> AddMessageAsync(
        int conversationId,
        CopilotMessageRole role,
        string content,
        CancellationToken cancellationToken = default);
}

public class CopilotConversationService(
    ApplicationDbContext dbContext,
    ILogger<CopilotConversationService> logger) : ICopilotConversationService
{
    public async Task<int> AddMessageAsync(int conversationId, CopilotMessageRole role, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "A message cannot be empty.",
                nameof(content));
        }

        var nextSequenceNumber = await dbContext.CopilotMessages
            .Where(message => message.ConversationId == conversationId)
            .Select(message => (int?)message.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var message = new CopilotMessage
        {
            ConversationId = conversationId,
            Role = role,
            Content = content.Trim(),
            SequenceNumber = nextSequenceNumber + 1,
            CreatedUtc = DateTime.UtcNow
        };

        dbContext.CopilotMessages.Add(message);

        var conversation = await dbContext.CopilotConversations
            .FirstAsync(
                item => item.Id == conversationId,
                cancellationToken);

        conversation.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return message.Id;
    }

    public async Task<CopilotConversation> GetOrCreateConversationAsync(CopilotRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ConversationId is not null)
        {
            var existingConversation = await dbContext.CopilotConversations
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Id == request.ConversationId &&
                        conversation.UserId == userId,
                    cancellationToken);

            if (existingConversation is not null)
            {
                return existingConversation;
            }

            logger.LogWarning(
                "Conversation {ConversationId} was not found for the current user.",
                request.ConversationId);
        }

        var contextType = MapContextType(request.Context);
        var contextId = ResolveContextId(request.Context);
        var title = BuildConversationTitle(request);

        var conversation = new CopilotConversation
        {
            UserId = userId,
            ContextType = contextType,
            ContextId = contextId,
            Title = title,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        dbContext.CopilotConversations.Add(conversation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    private static CopilotContextType MapContextType(CopilotPageContext? context)
    {
        if (context is null)
        {
            return CopilotContextType.General;
        }

        if (string.Equals(context.EntityType, "Document", StringComparison.OrdinalIgnoreCase))
        {
            return CopilotContextType.Document;
        }

        if (string.Equals(context.EntityType, "Request", StringComparison.OrdinalIgnoreCase))
        {
            return CopilotContextType.Request;
        }

        if (!string.IsNullOrWhiteSpace(context.PageTitle) ||
            !string.IsNullOrWhiteSpace(context.Area))
        {
            return CopilotContextType.Page;
        }

        return CopilotContextType.General;
    }

    private static string? ResolveContextId(CopilotPageContext? context)
    {
        if (!string.IsNullOrWhiteSpace(context?.EntityId))
        {
            return context.EntityId;
        }

        if (!string.IsNullOrWhiteSpace(context?.PageTitle))
        {
            return context.PageTitle;
        }

        if (!string.IsNullOrWhiteSpace(context?.Area))
        {
            return context.Area;
        }

        return null;
    }

    private static string BuildConversationTitle(CopilotRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Context?.PageTitle))
        {
            return request.Context.PageTitle;
        }

        var trimmedMessage = request.Message.Trim();

        return trimmedMessage.Length <= 60
            ? trimmedMessage
            : $"{trimmedMessage[..60]}...";
    }
}
