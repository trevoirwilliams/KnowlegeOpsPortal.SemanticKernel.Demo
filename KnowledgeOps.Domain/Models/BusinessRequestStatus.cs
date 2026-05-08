using System;

namespace KnowledgeOps.Domain.Models;

public enum BusinessRequestStatus
{
    New,
    InReview,
    WaitingForInformation,
    Approved,
    Rejected,
    Completed
}
