using System;

namespace KnowledgeOps.Domain.Models.Enums;

public enum BusinessRequestStatus
{
    New,
    InReview,
    WaitingForInformation,
    Approved,
    Rejected,
    Completed
}
