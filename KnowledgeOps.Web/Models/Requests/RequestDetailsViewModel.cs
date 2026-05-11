using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;

namespace KnowledgeOps.Web.Models.Requests;

public sealed class RequestDetailsViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public string RequestedBy { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string BusinessJustification { get; init; } = string.Empty;

    public BusinessRequestStatus Status { get; init; }

    public BusinessRequestImpact Impact { get; init; }

    public string Urgency { get; init; } = string.Empty;

    public DateTime SubmittedOnUtc { get; init; }

    public DateTime RequiredByUtc { get; init; }

    public string? AssignedTo { get; init; }
}
