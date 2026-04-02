namespace Agent.Runtime.Persistence;

public sealed class AgentMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; init; }

    public Guid? AgentRunId { get; init; }

    public int SequenceNumber { get; init; }

    public string AgentName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
