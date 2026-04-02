namespace Agent.Runtime.Persistence;

public sealed class ToolInvocation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public string InputSummary { get; set; } = string.Empty;

    public string OutputSummary { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string Status { get; set; } = ToolInvocationStatus.Started;

    public long DurationMs { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public static class ToolInvocationStatus
{
    public const string Started = "Started";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
