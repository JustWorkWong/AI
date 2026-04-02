using Shared.Contracts.Common;

namespace Shared.Contracts.Returns;

public sealed record ReturnOrderDto(
    Guid ReturnOrderId,
    string ReturnCode,
    string QualityState,
    string Status,
    string Notes);

public sealed record HistoricalCaseDto(
    Guid CaseId,
    string Condition,
    string Outcome);

public sealed record DispositionSuggestionDto(
    Guid ReturnOrderId,
    string SuggestedOutcome,
    string RiskLevel,
    IReadOnlyList<CitationDto> Citations,
    string ApprovalStatus);

public sealed record ExecuteDispositionRequest(string IdempotencyKey);

public sealed record DispositionExecutionResultDto(
    Guid WorkflowInstanceId,
    string Status,
    Guid? ApprovalReferenceId,
    string? Outcome);

public sealed record ToolInvocationDto(
    Guid ToolInvocationId,
    string ToolName,
    string Status,
    string TraceId,
    long DurationMs,
    string InputSummary,
    string OutputSummary,
    string? ErrorMessage);

public sealed record WorkflowCheckpointDto(
    Guid CheckpointId,
    int Superstep,
    string CheckpointType,
    string StateJson);

public sealed record DispositionExecutionTraceDto(
    Guid WorkflowInstanceId,
    string WorkflowCode,
    string Status,
    Guid? ApprovalReferenceId,
    IReadOnlyList<ToolInvocationDto> ToolInvocations,
    IReadOnlyList<WorkflowCheckpointDto> Checkpoints);

public sealed record RequestDispositionApproval(
    Guid ReturnOrderId,
    string SuggestedOutcome);

public sealed record ApplyDispositionCommand(
    Guid ReturnOrderId,
    string Outcome,
    string IdempotencyKey);

public sealed record ReturnWorkbenchViewDto(
    ReturnOrderDto Order,
    DispositionSuggestionDto Suggestion);
