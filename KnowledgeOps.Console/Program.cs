using KnowledgeOps.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: false);
builder.Services.AddKnowledgeOpsAI(builder.Configuration);
using var host = builder.Build();

var chat = host.Services.GetRequiredService<IChatCompletionService>();
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
    var response = await chat.GetChatMessageContentAsync(history);
    var text = response.Content ?? string.Empty;
    history.AddMessage(response.Role, text);

    Console.WriteLine($"Assistant > {text}");
}
