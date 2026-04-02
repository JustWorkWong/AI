using Shared.Contracts.Common;

namespace Shared.Contracts.Returns;

public sealed record DispositionSuggestionDto(
    Guid ReturnOrderId,
    string SuggestedOutcome,
    string RiskLevel,
    IReadOnlyList<CitationDto> Citations,
    string ApprovalStatus);

public sealed record RequestDispositionApproval(
    Guid ReturnOrderId,
    string SuggestedOutcome);

public sealed record ApplyDispositionCommand(
    Guid ReturnOrderId,
    string Outcome,
    string IdempotencyKey);
