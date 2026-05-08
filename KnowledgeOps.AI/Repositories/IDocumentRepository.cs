using System;
using KnowledgeOps.AI.Models;

namespace KnowledgeOps.AI.Repositories;

public interface IDocumentRepository
{
    Task<IReadOnlyList<KnowledgeDocument>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<KnowledgeDocument?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
}

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private static readonly IReadOnlyList<KnowledgeDocument> Documents =
    [
        new()
        {
            Id = "DOC-1001",
            Title = "Employee Onboarding Guide",
            Category = "Human Resources",
            Department = "People Operations",
            Owner = "HR Operations Team",
            Status = KnowledgeDocumentStatus.Approved,
            LastReviewedOn = new DateOnly(2026, 4, 12),
            Summary = "Standard onboarding guidance for new employees, including required forms, first-week expectations, and orientation steps.",
            Tags = ["onboarding", "hr", "employee-experience"]
        },
        new()
        {
            Id = "DOC-1002",
            Title = "Vendor Risk Review Checklist",
            Category = "Compliance",
            Department = "Risk Management",
            Owner = "Compliance Office",
            Status = KnowledgeDocumentStatus.NeedsReview,
            LastReviewedOn = new DateOnly(2026, 2, 28),
            Summary = "Checklist used to evaluate vendor risk before approving a new third-party service provider.",
            Tags = ["vendors", "risk", "compliance"]
        },
        new()
        {
            Id = "DOC-1003",
            Title = "Incident Response Communication Plan",
            Category = "Operations",
            Department = "IT Operations",
            Owner = "Service Reliability Team",
            Status = KnowledgeDocumentStatus.Draft,
            LastReviewedOn = new DateOnly(2026, 3, 18),
            Summary = "Communication plan for coordinating internal updates during service incidents and operational disruptions.",
            Tags = ["incident-response", "operations", "communications"]
        },
        new()
        {
            Id = "DOC-1004",
            Title = "Customer Data Handling Policy",
            Category = "Security",
            Department = "Information Security",
            Owner = "Security Governance Team",
            Status = KnowledgeDocumentStatus.Approved,
            LastReviewedOn = new DateOnly(2026, 1, 22),
            Summary = "Policy that explains how customer data should be classified, handled, shared, and retained across internal systems.",
            Tags = ["security", "data-protection", "governance"]
        }
    ];

    public Task<IReadOnlyList<KnowledgeDocument>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Documents);
    }

    public Task<KnowledgeDocument?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var document = Documents.FirstOrDefault(document =>
            string.Equals(document.Id, id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(document);
    }
}
