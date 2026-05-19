using System;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using KnowledgeOps.Web.Models.Copilot;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Web.Services;

public interface ICopilotConversationService
{
    Task<CopilotConversation> GetOrCreateConversationAsync(
        CopilotRequest request,
        string? userId,
        TimeSpan relevanceWindow,
        CancellationToken cancellationToken = default);

    Task<int> AddMessageAsync(
        int conversationId,
        CopilotMessageRole role,
        string content,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CopilotMessage>> GetMessagesForModelAsync(
        int conversationId,
        string? userId,
        int maxMessages,
        int summarizedThroughSequenceNumber,
        CancellationToken cancellationToken = default);
    
    Task<CopilotConversation?> GetLatestActiveConversationAsync(
    CopilotPageContext? context,
    string? userId,
    TimeSpan relevanceWindow,
    CancellationToken cancellationToken = default);

    Task<CopilotHistoryResponse> GetHistoryResponseAsync(
        CopilotPageContext? context,
        string? userId,
        TimeSpan relevanceWindow,
        int maxMessages,
        CancellationToken cancellationToken = default);

    Task<bool> ClearConversationAsync(
        int conversationId,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<int> DeleteConversationsOlderThanAsync(
        TimeSpan maxAge,
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

    public async Task<CopilotConversation> GetOrCreateConversationAsync(CopilotRequest request, string? userId, TimeSpan relevanceWindow, CancellationToken cancellationToken = default)
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

        var latestActiveConversation = await GetLatestActiveConversationAsync(   
            request.Context,
            userId,
            relevanceWindow,
            cancellationToken
        );

        if (latestActiveConversation is not null)
        {
            return latestActiveConversation;
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

    public async Task<IReadOnlyList<CopilotMessage>> GetMessagesForModelAsync(int conversationId, string? userId, int maxMessages, int summarizedThroughSequenceNumber, CancellationToken cancellationToken = default)
    {
        var conversationExists = await dbContext.CopilotConversations
            .AnyAsync(
                conversation =>
                    conversation.Id == conversationId &&
                    conversation.UserId == userId,
                cancellationToken);

        if (!conversationExists)
        {
            return [];
        }

        return await dbContext.CopilotMessages
            .Where(message =>
                message.ConversationId == conversationId &&
                message.SequenceNumber > summarizedThroughSequenceNumber)
            .OrderByDescending(message => message.SequenceNumber)
            .Take(maxMessages)
            .OrderBy(message => message.SequenceNumber)
            .ToListAsync(cancellationToken);
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

    public async Task<CopilotConversation?> GetLatestActiveConversationAsync(CopilotPageContext? context, string? userId, TimeSpan relevanceWindow, CancellationToken cancellationToken = default)
    {
        var contextType = MapContextType(context);
        var contextId = ResolveContextId(context);
        var cutoffUtc = DateTime.UtcNow.Subtract(relevanceWindow);

        return await dbContext.CopilotConversations
        .Where(conversation =>
            conversation.UserId == userId &&
            conversation.ContextType == contextType &&
            conversation.ContextId == contextId &&
            conversation.UpdatedUtc >= cutoffUtc)
        .OrderByDescending(conversation => conversation.UpdatedUtc)
        .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CopilotHistoryResponse> GetHistoryResponseAsync(CopilotPageContext? context, string? userId, TimeSpan relevanceWindow, int maxMessages, CancellationToken cancellationToken = default)
    {
        var conversation = await GetLatestActiveConversationAsync(
            context,
            userId,
            relevanceWindow,
            cancellationToken);

        if (conversation is null)
        {
            return new CopilotHistoryResponse();
        }

        var messages = await dbContext.CopilotMessages
        .Where(message => message.ConversationId == conversation.Id)
        .OrderByDescending(message => message.SequenceNumber)
        .Take(maxMessages)
        .OrderBy(message => message.SequenceNumber)
        .Select(message => new CopilotHistoryMessage
        {
            Role = message.Role == CopilotMessageRole.User ? "user" : "assistant",
            Content = message.Content,
            CreatedUtc = message.CreatedUtc
        })
        .ToListAsync(cancellationToken);

        return new CopilotHistoryResponse
        {
            ConversationId = conversation.Id,
            ContextSummary = conversation.Title,
            Messages = messages
        };


    }

    public async Task<bool> ClearConversationAsync(int conversationId, string? userId, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.CopilotConversations
            .Include(item => item.Messages)
            .FirstOrDefaultAsync(
                item => item.Id == conversationId && item.UserId == userId,
                cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        dbContext.CopilotConversations.Remove(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteConversationsOlderThanAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoffUtc = DateTime.UtcNow.Subtract(maxAge);

        var oldConversations = await dbContext.CopilotConversations
            .Where(conversation => conversation.UpdatedUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        if (oldConversations.Count == 0)
        {
            return 0;
        }

        dbContext.CopilotConversations.RemoveRange(oldConversations);

        await dbContext.SaveChangesAsync(cancellationToken);

        return oldConversations.Count;
    }
}
