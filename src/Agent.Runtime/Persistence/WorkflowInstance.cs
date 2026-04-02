namespace Agent.Runtime.Persistence;

public sealed class WorkflowInstance
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SessionId { get; set; }

    public string WorkflowCode { get; set; } = string.Empty;

    public string Status { get; set; } = WorkflowInstanceStatus.Created;

    public Guid? ApprovalReferenceId { get; set; }

    public int Version { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public List<WorkflowCheckpoint> Checkpoints { get; init; } = [];

    public void WaitForApproval(Guid? approvalReferenceId)
    {
        ApprovalReferenceId = approvalReferenceId;
        Status = WorkflowInstanceStatus.WaitingApproval;
        CompletedAtUtc = null;
        Version++;
    }

    public void ClaimApproval()
    {
        Status = WorkflowInstanceStatus.Approving;
        Version++;
    }

    public void Complete()
    {
        Status = WorkflowInstanceStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Version++;
    }

    public void CompleteApproval() => Complete();

    public void RejectApproval()
    {
        Status = WorkflowInstanceStatus.Rejected;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Version++;
    }

    public void Fail()
    {
        Status = WorkflowInstanceStatus.Failed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Version++;
    }
}

public static class WorkflowInstanceStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string WaitingApproval = "WaitingApproval";
    public const string Approving = "Approving";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
