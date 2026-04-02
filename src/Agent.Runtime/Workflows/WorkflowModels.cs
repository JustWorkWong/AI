namespace Agent.Runtime.Workflows;

public sealed record ReturnDispositionInput(Guid ReturnOrderId, string IdempotencyKey);

public sealed record ReturnOrderSnapshot(Guid ReturnOrderId, string QualityState);

public sealed record DispositionSuggestion(bool RequiresApproval, string Outcome);

public sealed record ApprovalReference(Guid ReferenceId);

public sealed record WorkflowResult(string Status, Guid? ApprovalReferenceId, string? Outcome)
{
    public static WorkflowResult WaitingForApproval(Guid approvalReferenceId) =>
        new("WaitingForApproval", approvalReferenceId, null);

    public static WorkflowResult Completed(string outcome) =>
        new("Completed", null, outcome);
}

public sealed record SopAssistInput(Guid SessionId, string OperationCode, string StepCode);

public sealed record SopAssistResponse(string Message);

public sealed record SopAssistResult(string Status, object? Payload)
{
    public static SopAssistResult ManualReviewRequired() => new("ManualReviewRequired", null);

    public static SopAssistResult Success(object response, object evidence) =>
        new("Success", new { Response = response, Evidence = evidence });
}

public sealed record RuntimeCheckpoint(string CheckpointType, object? Value);
