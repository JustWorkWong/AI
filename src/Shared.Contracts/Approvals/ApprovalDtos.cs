namespace Shared.Contracts.Approvals;

public sealed record ApprovalTaskDto(
    Guid ApprovalTaskId,
    string ApprovalType,
    string Status,
    string RequestedBy);

public sealed record SyncUserRequest(
    string ExternalSubject,
    string UserName,
    string DisplayName);
