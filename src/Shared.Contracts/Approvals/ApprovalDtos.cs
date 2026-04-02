namespace Shared.Contracts.Approvals;

public sealed record ApprovalTaskDto(
    Guid ApprovalTaskId,
    string ApprovalType,
    string Status,
    Guid AggregateId);

public sealed record ApprovalDecisionRequest(
    string Action,
    string Actor);

public sealed record ApprovalDecisionCommand(
    string Action,
    string Actor);

public sealed record SyncUserRequest(
    string ExternalSubject,
    string UserName,
    string DisplayName);
