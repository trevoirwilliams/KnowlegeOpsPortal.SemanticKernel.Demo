using System;
using KnowledgeOps.AI.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace KnowledgeOps.AI.Services;

public interface IKnowledgeOpsChatClient
{
    Task<string> ReplyAsync(ChatHistory history, CancellationToken cancellationToken = default);
    Task<string> GetCurrentDateAsync(CancellationToken cancellationToken = default);
    Task<string> AskWithPromptAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> SummarizeRequestAsync(
    string requestTitle,
    string requestDetails,
    string audience,
    CancellationToken cancellationToken = default);
    Task<string> CreateOperationsBriefAsync(
    string requestDetails,
    CancellationToken cancellationToken = default);
    Task<string> CreateRequestBriefFromPluginAsync(
    string requestId,
    CancellationToken cancellationToken = default);
    Task<string> GetBusinessRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task<string> GetOpenBusinessRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<string> SummarizeConversationHistoryAsync(
    string existingSummary,
    string conversationText,
    CancellationToken cancellationToken = default);
}

internal sealed class KnowledgeOpsChatClient(
    IChatCompletionService chatCompletionService,
    Kernel kernel) : IKnowledgeOpsChatClient
{
    public async Task<string> AskWithPromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var result = await kernel.InvokePromptAsync(
            prompt,
            cancellationToken: cancellationToken);

        return result.ToString();
    }

    public async Task<string> GetCurrentDateAsync(CancellationToken cancellationToken = default)
    {
        var result = await kernel.InvokeAsync("Time", "Today", cancellationToken: cancellationToken);
        return result.ToString();
    }

    public async Task<string> ReplyAsync(ChatHistory history, CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: settings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        return result.Content ?? string.Empty;
    }

    public async Task<string> SummarizeRequestAsync(
    string requestTitle,
    string requestDetails,
    string audience,
    CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["requestTitle"] = requestTitle,
            ["requestDetails"] = requestDetails,
            ["audience"] = audience
        };

        var result = await kernel.InvokePromptAsync(
            KnowledgeOpsPromptTemplates.RequestSummary,
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }

    public async Task<string> CreateOperationsBriefAsync(
    string requestDetails,
    CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["requestDetails"] = requestDetails
        };

        var result = await kernel.InvokeAsync(
            "RequestOperations",
            "CreateOperationsBrief",
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }

    public async Task<string> CreateRequestBriefFromPluginAsync(
    string requestId,
    CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["requestId"] = requestId
        };

        var result = await kernel.InvokePromptAsync(
            KnowledgeOpsPromptTemplates.RequestBriefFromPlugin,
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }

    public async Task<string> GetBusinessRequestAsync(
    string requestId,
    CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["requestId"] = requestId
        };

        var result = await kernel.InvokeAsync(
            "BusinessRequests",
            "get_request_by_id",
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }


    public async Task<string> GetOpenBusinessRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await kernel.InvokeAsync(
            "BusinessRequests",
            "get_open_business_requests",
            cancellationToken: cancellationToken);

        return result.ToString();
    }

    public async Task<string> SummarizeConversationHistoryAsync(
        string existingSummary,
        string conversationText,
        CancellationToken cancellationToken = default)
    {
        var prompt = KnowledgeOpsPromptTemplates.ConversationHistorySummary;

        var arguments = new KernelArguments
        {
            ["existingSummary"] = string.IsNullOrWhiteSpace(existingSummary)
                ? "No previous summary."
                : existingSummary,
            ["conversationText"] = conversationText
        };

        var result = await kernel.InvokePromptAsync(
            prompt,
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }
}

