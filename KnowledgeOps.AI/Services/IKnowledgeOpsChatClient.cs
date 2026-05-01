using System;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KnowledgeOps.AI.Services;

public interface IKnowledgeOpsChatClient
{
    Task<string> ReplyAsync(ChatHistory history, CancellationToken cancellationToken = default);
}

internal sealed class KnowledgeOpsChatClient(
    IChatCompletionService chatCompletionService,
    Kernel kernel) : IKnowledgeOpsChatClient
{
    public async Task<string> ReplyAsync(ChatHistory history, CancellationToken cancellationToken = default)
    {
        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            kernel: kernel,
            cancellationToken: cancellationToken);

        return result.Content ?? string.Empty;
    }
}

