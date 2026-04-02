using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;

namespace Agent.Runtime.Tests;

public sealed class InMemoryToolInvocationStore : IToolInvocationStore
{
    public List<ToolInvocation> Items { get; } = [];

    public Task SaveAsync(ToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        Items.Add(invocation);
        return Task.CompletedTask;
    }
}
