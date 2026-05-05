using System;
using KnowledgeOps.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.Core;

namespace KnowledgeOps.AI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKnowledgeOpsAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;

            var builder = Kernel.CreateBuilder();
            builder.Services.AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Information));
            builder.AddAzureOpenAIChatCompletion(
                options.DeploymentName,
                options.Endpoint,
                options.ApiKey);

            builder.Plugins.AddFromType<TimePlugin>("Time");
            builder.Plugins.AddFromType<ConversationSummaryPlugin>("Summarization");

            var kernel = builder.Build();

            var requestOperationsPluginPath = Path.Combine(
                AppContext.BaseDirectory,
                "Plugins",
                "RequestOperationsPlugin");

            kernel.ImportPluginFromPromptDirectory(
                requestOperationsPluginPath,
                "RequestOperations");

            return kernel;
        });

        services.AddSingleton(sp =>
            sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());

        services.AddSingleton<IKnowledgeOpsChatClient, KnowledgeOpsChatClient>();

        return services;
    }
}