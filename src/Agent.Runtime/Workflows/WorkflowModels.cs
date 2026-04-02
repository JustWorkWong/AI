namespace Agent.Runtime.Workflows;

public sealed record ReturnDispositionInput(Guid ReturnOrderId, string IdempotencyKey);

public sealed record GetReturnOrderToolInput(Guid ReturnOrderId);

public sealed record SearchReturnSopToolInput(Guid ReturnOrderId, ReturnOrderSnapshot ReturnOrder);

public sealed record SearchHistoricalCasesToolInput(Guid ReturnOrderId, ReturnOrderSnapshot ReturnOrder);

public sealed record RequestDispositionApprovalToolInput(Guid ReturnOrderId, string Outcome);

public sealed record ApplyDispositionDecisionToolInput(Guid ReturnOrderId, string Outcome, string IdempotencyKey);

public sealed record ReturnOrderSnapshot(Guid ReturnOrderId, string QualityState);

public sealed record DispositionSuggestion(bool RequiresApproval, string Outcome);

public sealed record ApprovalReference(Guid ReferenceId);

public sealed record ApprovalCheckpointState(
    Guid ApprovalReferenceId,
    Guid ReturnOrderId,
    string Outcome,
    string IdempotencyKey);

public sealed record WorkflowResult(string Status, Guid? ApprovalReferenceId, string? Outcome)
{
    public static WorkflowResult WaitingForApproval(Guid approvalReferenceId) =>
        new("WaitingForApproval", approvalReferenceId, null);

    public static WorkflowResult Completed(string outcome) =>
        new("Completed", null, outcome);
}

public sealed record SopAssistInput(Guid SessionId, string OperationCode, string StepCode);

public sealed record SearchSopCandidatesToolInput(string OperationCode, string StepCode);

public sealed record RetrieveSopChunksToolInput(
    string OperationCode,
    string StepCode,
    IReadOnlyList<string> CandidateDocumentIds);

public sealed record RankSopEvidenceToolInput(
    string OperationCode,
    string StepCode,
    IReadOnlyList<string> Chunks);

public sealed record SopAssistResponse(string Message);

public sealed record SopAssistPayload(
    SopAssistResponse Response,
    IReadOnlyList<string> Evidence);

public sealed record SopAssistResult(string Status, object? Payload)
{
    public static SopAssistResult ManualReviewRequired() => new("ManualReviewRequired", null);

    public static SopAssistResult Success(SopAssistResponse response, IReadOnlyList<string> evidence) =>
        new("Success", new SopAssistPayload(response, evidence));
}

public sealed record RuntimeCheckpoint(string CheckpointType, object? Value);
