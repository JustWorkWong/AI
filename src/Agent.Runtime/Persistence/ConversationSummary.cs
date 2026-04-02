namespace Agent.Runtime.Persistence;

public sealed class ConversationSummary
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; init; }

    public string SummaryText { get; init; } = string.Empty;

    public int MessageCount { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
