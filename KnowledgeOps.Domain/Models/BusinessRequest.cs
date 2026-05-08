
namespace KnowledgeOps.Domain.Models;

public sealed class BusinessRequest
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Department { get; init; }

    public required string RequestedBy { get; init; }

    public required string Description { get; init; }

    public required string BusinessJustification { get; init; }

    public required BusinessRequestStatus Status { get; init; }

    public required BusinessRequestImpact Impact { get; init; }

    public required string Urgency { get; init; }

    public required DateTime SubmittedOnUtc { get; init; }

    public required DateTime RequiredByUtc { get; init; }

    public string? AssignedTo { get; init; }
}