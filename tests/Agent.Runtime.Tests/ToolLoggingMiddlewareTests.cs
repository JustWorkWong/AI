using Agent.Runtime.Observability;

namespace Agent.Runtime.Tests;

public sealed class ToolLoggingMiddlewareTests
{
    [Fact]
    public async Task Middleware_should_persist_tool_name_and_trace_id()
    {
        var store = new InMemoryToolInvocationStore();
        var middleware = new ToolLoggingMiddleware(store);

        await middleware.LogAsync("SearchSopTool", "trace-123", "{}");

        Assert.Single(store.Items, x => x.ToolName == "SearchSopTool" && x.TraceId == "trace-123");
    }
}
