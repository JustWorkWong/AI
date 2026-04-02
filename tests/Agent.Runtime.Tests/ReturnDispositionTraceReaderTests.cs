using Agent.Runtime.Persistence;
using Agent.Runtime.Services;
using Microsoft.EntityFrameworkCore;

namespace Agent.Runtime.Tests;

public sealed class ReturnDispositionTraceReaderTests
{
    [Fact]
    public async Task Get_trace_should_return_workflow_tools_and_checkpoints_in_order()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = Guid.NewGuid();

        db.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowInstanceId,
            SessionId = Guid.NewGuid(),
            WorkflowCode = "return-disposition-execute",
            Status = WorkflowInstanceStatus.WaitingApproval,
            ApprovalReferenceId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        });

        db.ToolInvocations.AddRange(
            new ToolInvocation
            {
                WorkflowInstanceId = workflowInstanceId,
                ToolName = "GetReturnOrderTool",
                TraceId = "trace-a",
                Status = ToolInvocationStatus.Completed,
                DurationMs = 12,
                InputSummary = "{}",
                OutputSummary = "order"
            },
            new ToolInvocation
            {
                WorkflowInstanceId = workflowInstanceId,
                ToolName = "RequestDispositionApprovalTool",
                TraceId = "trace-a",
                Status = ToolInvocationStatus.Completed,
                DurationMs = 7,
                InputSummary = "{}",
                OutputSummary = "approval"
            });

        db.WorkflowCheckpoints.Add(new WorkflowCheckpoint
        {
            WorkflowInstanceId = workflowInstanceId,
            Superstep = 1,
            CheckpointType = "approval",
            StateJson = "{\"approvalReferenceId\":\"11111111-1111-1111-1111-111111111111\"}"
        });

        await db.SaveChangesAsync();

        var reader = new ReturnDispositionTraceReader(db);

        var result = await reader.GetTraceAsync(workflowInstanceId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(workflowInstanceId, result!.WorkflowInstanceId);
        Assert.Equal("WaitingApproval", result.Status);
        Assert.Equal(2, result.ToolInvocations.Count);
        Assert.Equal("GetReturnOrderTool", result.ToolInvocations[0].ToolName);
        Assert.Single(result.Checkpoints);
        Assert.Equal("approval", result.Checkpoints[0].CheckpointType);
    }

    private static AgentRuntimeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentRuntimeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AgentRuntimeDbContext(options);
    }
}
