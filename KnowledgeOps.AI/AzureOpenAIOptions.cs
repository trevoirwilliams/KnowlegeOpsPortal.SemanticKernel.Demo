using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.AI;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required]
    public string DeploymentName { get; init; } = string.Empty;

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string EmbeddingDeploymentName { get; init; } = string.Empty;

    [Range(1, 4096)]
    public int EmbeddingDimensions { get; init; } = 1536;
}
