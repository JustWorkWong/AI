using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;

namespace Agent.Runtime.Tests;

public sealed class ConversationCompactorTests
{
    [Fact]
    public void Compactor_should_keep_recent_context_and_message_count()
    {
        var compactor = new ConversationCompactor();
        var workflowInstanceId = Guid.NewGuid();
        var messages = Enumerable.Range(1, 12)
            .Select(index => new AgentMessage
            {
                WorkflowInstanceId = workflowInstanceId,
                SequenceNumber = index,
                AgentName = "sop-assistant",
                Role = index == 1 ? "system" : "assistant",
                Content = $"msg-{index}"
            })
            .ToList();

        var summary = compactor.Compact(workflowInstanceId, messages);

        Assert.Equal(12, summary.MessageCount);
        Assert.Equal(workflowInstanceId, summary.WorkflowInstanceId);
        Assert.Equal(1, summary.StartSequenceNumber);
        Assert.Equal(12, summary.EndSequenceNumber);
        Assert.Contains("msg-12", summary.SummaryText);
        Assert.Contains("历史摘要:", summary.SummaryText);
        Assert.Contains("最近上下文:", summary.SummaryText);
    }
}
