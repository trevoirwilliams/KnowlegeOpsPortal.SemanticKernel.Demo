using System;
using KnowledgeOps.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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

            return builder.Build();
        });

        services.AddSingleton(sp =>
            sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());

        services.AddSingleton<IKnowledgeOpsChatClient, KnowledgeOpsChatClient>();

        return services;
    }
}