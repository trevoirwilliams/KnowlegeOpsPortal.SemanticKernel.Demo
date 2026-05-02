using KnowledgeOps.AI;
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
var history = new ChatHistory("You are a concise, helpful assistant for business app demos.");

Console.WriteLine("Type messages. Type exit to quit.");
while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    history.AddUserMessage(input);
    var response = await chat.ReplyAsync(history);
    history.AddAssistantMessage(response);
    Console.WriteLine($"Assistant > {response}");
}
