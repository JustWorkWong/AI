using Agent.Runtime.Streaming;

namespace Agent.Runtime.Tests;

public sealed class AgUiEventMapperTests
{
    [Fact]
    public void Mapper_should_convert_tool_start_to_contract_event()
    {
        var workflowInstanceId = Guid.NewGuid();
        var evt = AgUiEventMapper.MapToolStarted("SearchSopTool", "trace-123", workflowInstanceId, Guid.Empty);

        Assert.Equal("tool.started", evt.Type);
        Assert.Equal("trace-123", evt.TraceId);
        Assert.Equal(workflowInstanceId, evt.WorkflowInstanceId);
    }
}
