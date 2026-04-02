using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;

namespace Agent.Runtime.Tests;

public sealed class ToolLoggingMiddlewareTests
{
    [Fact]
    public async Task Middleware_should_persist_tool_name_and_trace_id()
    {
        var store = new InMemoryToolInvocationStore();
        var middleware = new ToolLoggingMiddleware(store);

        await middleware.LogAsync("SearchSopTool", "trace-123", "{}");

        var invocation = Assert.Single(store.Items);
        Assert.Equal("SearchSopTool", invocation.ToolName);
        Assert.Equal("trace-123", invocation.TraceId);
        Assert.Equal(ToolInvocationStatus.Started, invocation.Status);
    }

    [Fact]
    public async Task Middleware_should_capture_completion_output()
    {
        var store = new InMemoryToolInvocationStore();
        var middleware = new ToolLoggingMiddleware(store);

        var output = await middleware.ExecuteAsync(
            "RetrieveSopChunksTool",
            "trace-456",
            "{ step: 1 }",
            _ => Task.FromResult("{ chunks: 3 }"),
            Guid.NewGuid());

        Assert.Equal("{ chunks: 3 }", output);

        var invocation = Assert.Single(store.Items);
        Assert.Equal(ToolInvocationStatus.Completed, invocation.Status);
        Assert.Equal("{ chunks: 3 }", invocation.OutputSummary);
        Assert.NotNull(invocation.CompletedAtUtc);
    }

    [Fact]
    public async Task Middleware_should_mark_tool_as_failed_when_delegate_throws()
    {
        var store = new InMemoryToolInvocationStore();
        var middleware = new ToolLoggingMiddleware(store);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                "ApplyDispositionDecisionTool",
                "trace-789",
                "{ outcome: 'scrap' }",
                _ => Task.FromException<string>(new InvalidOperationException("boom")),
                Guid.NewGuid()));

        Assert.Equal("boom", error.Message);

        var invocation = Assert.Single(store.Items);
        Assert.Equal(ToolInvocationStatus.Failed, invocation.Status);
        Assert.Equal("boom", invocation.ErrorMessage);
        Assert.NotNull(invocation.CompletedAtUtc);
    }
}
