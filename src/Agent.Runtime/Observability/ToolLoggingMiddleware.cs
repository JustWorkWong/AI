using Agent.Runtime.Persistence;
using System.Diagnostics;

namespace Agent.Runtime.Observability;

public sealed class ToolLoggingMiddleware(IToolInvocationStore store)
{
    public async Task LogAsync(
        string toolName,
        string traceId,
        string inputSummary,
        CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(
            new ToolInvocation
            {
                ToolName = toolName,
                TraceId = traceId,
                InputSummary = inputSummary
            },
            cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(
        string toolName,
        string traceId,
        string inputSummary,
        Func<CancellationToken, Task<T>> next,
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var invocation = new ToolInvocation
        {
            WorkflowInstanceId = workflowInstanceId,
            ToolName = toolName,
            TraceId = traceId,
            InputSummary = inputSummary
        };

        await store.SaveAsync(invocation, cancellationToken);

        try
        {
            var output = await next(cancellationToken);
            stopwatch.Stop();

            invocation.OutputSummary = output?.ToString() ?? string.Empty;
            invocation.Status = ToolInvocationStatus.Completed;
            invocation.DurationMs = stopwatch.ElapsedMilliseconds;
            invocation.CompletedAtUtc = DateTimeOffset.UtcNow;

            await store.UpdateAsync(invocation, cancellationToken);

            return output;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            invocation.OutputSummary = string.Empty;
            invocation.ErrorMessage = ex.Message;
            invocation.Status = ToolInvocationStatus.Failed;
            invocation.DurationMs = stopwatch.ElapsedMilliseconds;
            invocation.CompletedAtUtc = DateTimeOffset.UtcNow;

            await store.UpdateAsync(invocation, cancellationToken);

            throw;
        }
    }
}
