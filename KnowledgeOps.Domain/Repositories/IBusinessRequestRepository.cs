using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Models.Enums;

namespace KnowledgeOps.Domain.Repositories;

public interface IBusinessRequestRepository
{
    Task<BusinessRequest?> GetByIdAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessRequest>> GetOpenRequestsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class InMemoryBusinessRequestRepository : IBusinessRequestRepository
{
    private static readonly IReadOnlyList<BusinessRequest> Requests =
    [
        new BusinessRequest
        {
            Id = "REQ-1001",
            Title = "Approve Azure OpenAI access for support knowledge assistant",
            Department = "Customer Support",
            RequestedBy = "Alicia Morgan",
            Description = "The support team wants access to Azure OpenAI so they can build an internal assistant that helps agents answer product support questions faster.",
            BusinessJustification = "Support response times have increased because agents must search multiple systems manually. The assistant should reduce lookup time and improve answer consistency.",
            Status = BusinessRequestStatus.InReview,
            Impact = BusinessRequestImpact.High,
            Urgency = "Medium",
            SubmittedOnUtc = DateTime.UtcNow.AddDays(-5),
            RequiredByUtc = DateTime.UtcNow.AddDays(10),
            AssignedTo = "Operations Review Team"
        },
        new BusinessRequest
        {
            Id = "REQ-1002",
            Title = "Create document intake workflow for compliance PDFs",
            Department = "Compliance",
            RequestedBy = "Marcus Bennett",
            Description = "The compliance team needs a workflow for uploading policy PDFs, extracting text, and making the content searchable for internal review.",
            BusinessJustification = "Compliance analysts spend too much time manually checking policy documents. A searchable intake workflow would improve review speed and reduce missed obligations.",
            Status = BusinessRequestStatus.New,
            Impact = BusinessRequestImpact.Critical,
            Urgency = "High",
            SubmittedOnUtc = DateTime.UtcNow.AddDays(-2),
            RequiredByUtc = DateTime.UtcNow.AddDays(5),
            AssignedTo = null
        },
        new BusinessRequest
        {
            Id = "REQ-1003",
            Title = "Summarize weekly vendor onboarding requests",
            Department = "Procurement",
            RequestedBy = "Danielle Price",
            Description = "The procurement team wants a weekly summary of vendor onboarding requests, including risks, missing information, and recommended next steps.",
            BusinessJustification = "Procurement managers need a faster way to identify vendor onboarding delays and prioritize follow-up actions.",
            Status = BusinessRequestStatus.WaitingForInformation,
            Impact = BusinessRequestImpact.Medium,
            Urgency = "Medium",
            SubmittedOnUtc = DateTime.UtcNow.AddDays(-12),
            RequiredByUtc = DateTime.UtcNow.AddDays(3),
            AssignedTo = "Vendor Management"
        }
    ];

    public Task<BusinessRequest?> GetByIdAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        BusinessRequest? request = Requests.FirstOrDefault(request =>
            string.Equals(request.Id, requestId.Trim(), StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(request);
    }

    public Task<IReadOnlyList<BusinessRequest>> GetOpenRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        BusinessRequestStatus[] closedStatuses =
        [
            BusinessRequestStatus.Approved,
            BusinessRequestStatus.Rejected,
            BusinessRequestStatus.Completed
        ];

        IReadOnlyList<BusinessRequest> openRequests = Requests
            .Where(request => !closedStatuses.Contains(request.Status))
            .OrderBy(request => request.RequiredByUtc)
            .ToList();

        return Task.FromResult(openRequests);
    }
}

