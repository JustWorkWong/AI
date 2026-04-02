namespace Agent.Runtime.Persistence;

public sealed class WorkflowCheckpoint
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; init; }

    public int Superstep { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string StateJson { get; init; } = "{}";

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
