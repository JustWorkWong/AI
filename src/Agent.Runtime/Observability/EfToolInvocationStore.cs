using Agent.Runtime.Persistence;

namespace Agent.Runtime.Observability;

public sealed class EfToolInvocationStore(AgentRuntimeDbContext dbContext) : IToolInvocationStore
{
    public async Task SaveAsync(ToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        dbContext.ToolInvocations.Add(invocation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
