using Agent.Runtime.Persistence;

namespace Agent.Runtime.Observability;

public interface IToolInvocationStore
{
    Task SaveAsync(ToolInvocation invocation, CancellationToken cancellationToken = default);

    Task UpdateAsync(ToolInvocation invocation, CancellationToken cancellationToken = default);
}
