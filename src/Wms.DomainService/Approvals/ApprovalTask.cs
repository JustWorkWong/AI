namespace Wms.DomainService.Approvals;

public sealed class ApprovalTask
{
    public ApprovalTask(Guid id, string approvalType, Guid aggregateId)
    {
        Id = id;
        ApprovalType = approvalType;
        AggregateId = aggregateId;
        Status = "Pending";
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private ApprovalTask()
    {
    }

    public Guid Id { get; private set; }

    public string ApprovalType { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsPending => string.Equals(Status, "Pending", StringComparison.Ordinal);

    public void MarkApproved()
    {
        Status = "Approved";
    }

    public void MarkRejected()
    {
        Status = "Rejected";
    }
}
