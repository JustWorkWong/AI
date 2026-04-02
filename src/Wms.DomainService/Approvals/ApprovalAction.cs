namespace Wms.DomainService.Approvals;

public sealed class ApprovalAction
{
    public ApprovalAction(Guid id, Guid approvalTaskId, string action, string actor)
    {
        Id = id;
        ApprovalTaskId = approvalTaskId;
        Action = action;
        Actor = actor;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private ApprovalAction()
    {
    }

    public Guid Id { get; private set; }

    public Guid ApprovalTaskId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string Actor { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
