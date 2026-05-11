using KnowledgeOps.AI.Services;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Web.Models.Copilot;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KnowledgeOps.Web.Services;

public interface ICopilotService
{
    Task<CopilotResponse> GetResponseAsync(
            CopilotRequest request,
            CancellationToken cancellationToken = default);
}

public sealed class CopilotService(
    IKnowledgeOpsChatClient chatClient,
    ICopilotConversationService conversationService,
    ICopilotHistorySummarizer historySummarizer,
    ICurrentUserService currentUserService,
    IOptions<CopilotHistoryOptions> historyOptions,
    ILogger<CopilotService> logger) : ICopilotService
{
    public async Task<CopilotResponse> GetResponseAsync(
        CopilotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "A message is required before the copilot can respond.",
                nameof(request));
        }

        var userId = currentUserService.UserId;
        var conversation = await conversationService.GetOrCreateConversationAsync(
            request,
            userId,
            cancellationToken);

        await conversationService.AddMessageAsync(
            conversation.Id,
            CopilotMessageRole.User,
            request.Message,
            cancellationToken);

        await historySummarizer.SummarizeIfNeededAsync(
        conversation,
        cancellationToken);

        var summarizedThroughSequence = conversation.SummarizedThroughSequenceNumber ?? 0;

        var recentMessages = await conversationService.GetMessagesForModelAsync(
            conversation.Id,
            userId,
            historyOptions.Value.MaxModelMessages,
            summarizedThroughSequence,
            cancellationToken);


        var history = new ChatHistory();

        history.AddSystemMessage(BuildSystemMessage(request.Context));

        if (request.Context is not null)
        {
            history.AddUserMessage(BuildContextMessage(request.Context));
        }

        if (!string.IsNullOrWhiteSpace(conversation.Summary))
        {
            history.AddSystemMessage($"""
            Conversation memory summary from earlier in this same chat:
            {conversation.Summary}
            """);
        }

        foreach (var previousMessage in recentMessages)
        {
            AddPersistedMessageToHistory(history, previousMessage);
        }

        history.AddUserMessage(request.Message.Trim());

        logger.LogInformation(
            "Sending copilot request. Area: {Area}, EntityType: {EntityType}, EntityId: {EntityId}",
            request.Context?.Area,
            request.Context?.EntityType,
            request.Context?.EntityId);

        var response = await chatClient.ReplyAsync(history, cancellationToken);

        var assistantMessage = string.IsNullOrWhiteSpace(response)
        ? "I could not generate a response for that request."
        : response;

        await conversationService.AddMessageAsync(
        conversation.Id,
        CopilotMessageRole.Assistant,
        assistantMessage,
        cancellationToken);

        return new CopilotResponse
        {
            Message = string.IsNullOrWhiteSpace(response)
                ? "I could not generate a response for that request."
                : response,
            ContextSummary = BuildContextSummary(request.Context),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void AddPersistedMessageToHistory(
    ChatHistory history,
    CopilotMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Content))
        {
            return;
        }

        switch (message.Role)
        {
            case CopilotMessageRole.User:
                history.AddUserMessage(message.Content);
                break;

            case CopilotMessageRole.Assistant:
                history.AddAssistantMessage(message.Content);
                break;

            case CopilotMessageRole.System:
                history.AddSystemMessage(message.Content);
                break;
        }
    }

    private static string BuildSystemMessage(CopilotPageContext? context)
    {
        var area = string.IsNullOrWhiteSpace(context?.Area)
            ? "the KnowledgeOps portal"
            : $"the {context.Area} area of the KnowledgeOps portal";

        return $"""
        You are the embedded copilot for {area}.

        Your job is to help users understand information, summarize business context,
        clarify next steps, and reason over the current portal workflow.

        Follow these rules:
        - Stay focused on the user's current page and task.
        - Use the supplied page context when it is relevant.
        - Do not claim that you accessed documents, databases, or systems unless that information was supplied.
        - Do not invent missing business facts.
        - If the available context is not enough, say what additional information would be needed.
        - Keep responses professional, practical, and concise.
        """;
    }

    private static string BuildContextMessage(CopilotPageContext context)
    {
        var metadata = context.Metadata.Count == 0
            ? "No additional metadata was supplied."
            : string.Join(
                Environment.NewLine,
                context.Metadata.Select(item => $"- {item.Key}: {item.Value}"));

        return $"""
        Current portal context:

        Area: {ValueOrFallback(context.Area)}
        Page title: {ValueOrFallback(context.PageTitle)}
        Entity type: {ValueOrFallback(context.EntityType)}
        Entity ID: {ValueOrFallback(context.EntityId)}
        Summary: {ValueOrFallback(context.Summary)}

        Metadata:
        {metadata}
        """;
    }

    private static string? BuildContextSummary(CopilotPageContext? context)
    {
        if (context is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(context.EntityType) &&
            !string.IsNullOrWhiteSpace(context.EntityId))
        {
            return $"{context.EntityType}: {context.EntityId}";
        }

        return context.PageTitle;
    }

    private static string ValueOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not supplied" : value;
    }
}
