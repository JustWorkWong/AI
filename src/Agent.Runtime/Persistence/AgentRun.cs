namespace Agent.Runtime.Persistence;

public sealed class AgentRun
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; init; }

    public string AgentName { get; set; } = string.Empty;

    public string Status { get; set; } = AgentRunStatus.Created;

    public string ModelProfileCode { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public static class AgentRunStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
