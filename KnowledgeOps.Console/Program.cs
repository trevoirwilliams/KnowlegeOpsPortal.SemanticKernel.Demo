using KnowledgeOps.AI;
using KnowledgeOps.AI.Prompts;
using KnowledgeOps.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: false);
builder.Services.AddKnowledgeOpsAI(builder.Configuration);
using var host = builder.Build();

var chat = host.Services.GetRequiredService<IKnowledgeOpsChatClient>();
// var history = new ChatHistory(KnowledgeOpsPromptTemplates.SystemPrompt);
var history = KnowledgeOpsPromptTemplates.CreateOperationsAssistantHistory();

Console.WriteLine("Type messages. Type exit to quit.");
while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (input.Equals("/today", StringComparison.OrdinalIgnoreCase))
    {
        var today = await chat.GetCurrentDateAsync();
        Console.WriteLine($"System > {today}");
        continue;
    }

    if (input.Equals("/prompt-test", StringComparison.OrdinalIgnoreCase))
    {
        var request = """
            A department submitted a vendor onboarding request, but the tax document is missing.
            The requester says the vendor needs access by Friday.
        """;

        var weakPrompt = $"Summarize this request: {request}";

        var betterPrompt = $"""
            You are a KnowledgeOps assistant helping an operations analyst.
            Summarize the request below in 4 bullet points:
            - Business need
            - Missing information
            - Recommended next action
            - Risk if delayed

            Request:
            {request}
        """;

        Console.WriteLine("Weak Prompt Result:");
        Console.WriteLine(await chat.AskWithPromptAsync(weakPrompt));

        Console.WriteLine();
        Console.WriteLine("Application Prompt Result:");
        Console.WriteLine(await chat.AskWithPromptAsync(betterPrompt));

        continue;
    }

    if (input.Equals("/request-summary", StringComparison.OrdinalIgnoreCase))
    {
        var result = await chat.SummarizeRequestAsync(
            "Vendor onboarding request",
            "The vendor needs access by Friday, but the tax compliance document is missing.",
            "an operations manager");

        Console.WriteLine($"Assistant > {result}");
        continue;
    }

    if (input.Equals("/brief", StringComparison.OrdinalIgnoreCase))
    {
        var requestDetails = """
        A department submitted a software access request for a new contractor who starts tomorrow morning.
        The request says the contractor needs access to the document management system and the finance reporting dashboard.
        The manager's approval is attached, but the request does not say whether the contractor should have read-only access or editing permissions.
        """;

        var brief = await chat.CreateOperationsBriefAsync(requestDetails);

        Console.WriteLine($"Assistant > {brief}");
        continue;
    }

    if (input.StartsWith("/request ", StringComparison.OrdinalIgnoreCase))
    {
        var requestId = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(requestId))
        {
            Console.WriteLine("Assistant > Enter a request ID. Example: /request REQ-1001");
            continue;
        }

        var request = await chat.GetBusinessRequestAsync(requestId);

        Console.WriteLine($"Assistant > {request}");

        continue;
    }

    if (input.Equals("/requests", StringComparison.OrdinalIgnoreCase))
    {
        var requests = await chat.GetOpenBusinessRequestsAsync();

        Console.WriteLine($"Assistant > {requests}");

        continue;
    }

    history.AddUserMessage(input);
    var response = await chat.ReplyAsync(history);
    history.AddAssistantMessage(response);
    Console.WriteLine($"Assistant > {response}");
}
