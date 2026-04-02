using Agent.Runtime.Persistence;
using System.Text;

namespace Agent.Runtime.Observability;

public sealed class ConversationCompactor
{
    private const int RecentWindowSize = 8;

    public ConversationSummary Compact(Guid workflowInstanceId, IReadOnlyList<AgentMessage> messages)
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

        var ordered = messages
            .OrderBy(x => x.SequenceNumber)
            .ThenBy(x => x.CreatedAtUtc)
            .ToList();

        var recent = ordered.TakeLast(Math.Min(RecentWindowSize, ordered.Count)).ToList();
        var archived = ordered.Take(ordered.Count - recent.Count).ToList();
        var builder = new StringBuilder();

        if (archived.Count > 0)
        {
            builder.AppendLine("历史摘要:");

            foreach (var message in archived)
            {
                builder.Append('[')
                    .Append(message.Role)
                    .Append("] ")
                    .AppendLine(Trim(message.Content, 120));
            }
        }

        builder.AppendLine("最近上下文:");

        foreach (var message in recent)
        {
            builder.Append('[')
                .Append(message.Role)
                .Append("] ")
                .AppendLine(Trim(message.Content, 240));
        }

        return new ConversationSummary
        {
            WorkflowInstanceId = workflowInstanceId,
            MessageCount = messages.Count,
            StartSequenceNumber = ordered[0].SequenceNumber,
            EndSequenceNumber = ordered[^1].SequenceNumber,
            SummaryText = builder.ToString().TrimEnd()
        };
    }

    private static string Trim(string content, int maxLength)
    {
        return content.Length <= maxLength
            ? content
            : string.Concat(content.AsSpan(0, maxLength), "...");
    }
}
