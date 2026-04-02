namespace Shared.Contracts.Realtime;

public sealed record AgUiEvent(
    string Type,
    string TraceId,
    Guid WorkflowInstanceId,
    Guid SessionId,
    object Payload);
