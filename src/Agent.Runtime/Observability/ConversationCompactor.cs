using Agent.Runtime.Persistence;

namespace Agent.Runtime.Observability;

public sealed class ConversationCompactor
{
    public ConversationSummary Compact(Guid workflowInstanceId, IReadOnlyList<AgentMessage> messages, int keepRecentCount = 4)
    {
        if (messages.Count == 0)
        {
            return new ConversationSummary
            {
                WorkflowInstanceId = workflowInstanceId,
                SummaryText = "No conversation history.",
                MessageCount = 0
            };
        }

        var recentMessages = messages
            .Skip(Math.Max(0, messages.Count - keepRecentCount))
            .Select(x => $"{x.Role}:{x.Content}");

        return new ConversationSummary
        {
            WorkflowInstanceId = workflowInstanceId,
            MessageCount = messages.Count,
            SummaryText = string.Join(Environment.NewLine, recentMessages)
        };
    }
}
