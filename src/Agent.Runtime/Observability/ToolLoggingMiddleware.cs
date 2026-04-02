using Agent.Runtime.Persistence;
using System.Diagnostics;

namespace Agent.Runtime.Observability;

public sealed class ToolLoggingMiddleware(IToolInvocationStore store)
{
    public Task LogAsync(
        string toolName,
        string traceId,
        string inputSummary,
        CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(
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

        try
        {
            var output = await next(cancellationToken);
            stopwatch.Stop();

            await store.SaveAsync(
                new ToolInvocation
                {
                    WorkflowInstanceId = workflowInstanceId,
                    ToolName = toolName,
                    TraceId = traceId,
                    InputSummary = inputSummary,
                    OutputSummary = output?.ToString() ?? string.Empty,
                    Status = ToolInvocationStatus.Succeeded,
                    DurationMs = stopwatch.ElapsedMilliseconds
                },
                cancellationToken);

            return output;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await store.SaveAsync(
                new ToolInvocation
                {
                    WorkflowInstanceId = workflowInstanceId,
                    ToolName = toolName,
                    TraceId = traceId,
                    InputSummary = inputSummary,
                    OutputSummary = ex.Message,
                    Status = ToolInvocationStatus.Failed,
                    DurationMs = stopwatch.ElapsedMilliseconds
                },
                cancellationToken);

            throw;
        }
    }
}
