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

    public Task UpdateAsync(ToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        var index = Items.FindIndex(x => x.Id == invocation.Id);

        if (index >= 0)
        {
            Items[index] = invocation;
        }
        else
        {
            Items.Add(invocation);
        }

        return Task.CompletedTask;
    }
}
