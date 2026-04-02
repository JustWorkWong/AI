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
