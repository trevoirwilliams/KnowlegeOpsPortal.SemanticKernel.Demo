using KnowledgeOps.AI.Services;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;
using KnowledgeOps.Web.Models.Copilot;
using KnowledgeOps.Web.Models.Retrieval;
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
    ICurrentUserService currentUserService,
    IOptions<CopilotHistoryOptions> options,
    ICopilotHistorySummarizerService historySummarizer,
    IDocumentRetrievalService documentRetrievalService,
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
        var settings = options.Value;
        var userId = currentUserService.UserId;
        var conversation = await conversationService.GetOrCreateConversationAsync(
            request,
            userId,
            TimeSpan.FromHours(settings.ConversationRelevanceHours),
            cancellationToken);

        await conversationService.AddMessageAsync(
            conversation.Id,
            CopilotMessageRole.User,
            request.Message,
            cancellationToken);

        await historySummarizer.SummarizeIfNeededAsync(conversation, cancellationToken);

        var summarizedThroughSequence = conversation.SummarizedThroughSequenceNumber ?? 0;

        var recentMessages = await conversationService.GetMessagesForModelAsync(
            conversation.Id,
            userId,
            settings.MaxModelMessages,
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
        foreach (var message in recentMessages)
        {
            AddPersistedMessageToHistory(history, message);
        }

        KnowledgeRetrievalResult retrievedKnowledge =
            await documentRetrievalService.RetrieveRelevantKnowledgeAsync(
        new KnowledgeRetrievalRequest
        {
            Question = request.Message,
            EntityType = request.Context?.EntityType,
            EntityId = request.Context?.EntityId,
            UserId = userId,
            MaxResults = 5
        },
        cancellationToken);

        history.AddSystemMessage(BuildRetrievedKnowledgeMessage(retrievedKnowledge));

        history.AddUserMessage($"""
            User question:
            {request.Message.Trim()}

            Use the current portal context, conversation history, and retrieved document knowledge when relevant.
            """);

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
            ConversationId = conversation.Id,
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

        Your job is to help users understand information, summarize business context, clarify next steps, and reason over the current portal workflow.

        Follow these rules:
        - Stay focused on the user's current page and task.
        - Use the supplied page context when it is relevant.
        - Use retrieved document knowledge when it is supplied.
        - Treat retrieved document knowledge as the primary source for document-specific answers.
        - Do not claim that you accessed documents, databases, or systems unless that information was supplied.
        - Do not invent missing business facts.
        - If retrieved document knowledge does not contain enough information, say that the available document knowledge does not provide enough information.
        - If the user asks a general question that does not require document knowledge, answer normally but stay relevant to the portal workflow.
        - Keep responses professional, practical, and concise.
        """;
    }

    private static string BuildRetrievedKnowledgeMessage(
    KnowledgeRetrievalResult retrievalResult)
    {
        if (!retrievalResult.HasRelevantContent)
        {
            return """
            Retrieved document knowledge:
            No relevant document chunks were retrieved for this question.

            Instruction:
            If the user's question requires document-specific facts, say that the available document knowledge does not provide enough information.
            """;
        }

        var chunks = retrievalResult.Chunks
            .OrderBy(chunk => chunk.Score ?? double.MaxValue)
            .Select((chunk, index) => $"""
            Source {index + 1}
            File: {chunk.SourceFileName}
            Document ID: {chunk.DocumentId}
            Chunk ID: {chunk.ChunkId}
            Chunk Index: {chunk.ChunkIndex}
            Retrieval Score: {chunk.Score?.ToString("0.####") ?? "N/A"}
            Tags: {ValueOrFallback(chunk.DocumentTags)}

            Content:
            {chunk.Text}
            """);

        return $"""
        Retrieved document knowledge:
        The following chunks were retrieved from uploaded document knowledge for the user's current question.
        Use this knowledge when answering document-specific questions.

        {string.Join(Environment.NewLine + Environment.NewLine, chunks)}

        Grounding instructions:
        - Answer using the retrieved document knowledge when it is relevant.
        - Do not add document-specific facts that are not supported by the retrieved knowledge.
        - If the retrieved knowledge is insufficient, say that the available document knowledge does not provide enough information.
        - Do not mention retrieval scores to the user.
        - Do not expose internal chunk IDs unless the user asks for source details.
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
