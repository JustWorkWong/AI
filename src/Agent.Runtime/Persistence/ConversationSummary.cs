namespace Agent.Runtime.Persistence;

public sealed class ConversationSummary
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkflowInstanceId { get; set; }

    public string SummaryText { get; set; } = string.Empty;

    public int MessageCount { get; set; }

    public int StartSequenceNumber { get; set; }

    public int EndSequenceNumber { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
