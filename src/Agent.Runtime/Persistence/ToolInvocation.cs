namespace Agent.Runtime.Persistence;

public sealed class ToolInvocation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; init; }

    public string ToolName { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;

    public string InputSummary { get; init; } = string.Empty;

    public string OutputSummary { get; init; } = string.Empty;

    public string Status { get; init; } = ToolInvocationStatus.Succeeded;

    public long DurationMs { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public static class ToolInvocationStatus
{
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}
