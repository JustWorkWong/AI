using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;

namespace Agent.Runtime.Tests;

public sealed class ConversationCompactorTests
{
    [Fact]
    public void Compactor_should_keep_recent_context_and_message_count()
    {
        var compactor = new ConversationCompactor();
        var messages = Enumerable.Range(1, 12)
            .Select(index => new AgentMessage
            {
                AgentName = "sop-assistant",
                Role = "assistant",
                Content = $"msg-{index}"
            })
            .ToList();

        var summary = compactor.Compact(Guid.NewGuid(), messages);

        Assert.Equal(12, summary.MessageCount);
        Assert.Contains("msg-12", summary.SummaryText);
    }
}
