using Agent.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Returns;

namespace Agent.Runtime.Services;

public sealed class ReturnDispositionTraceReader(AgentRuntimeDbContext dbContext)
{
    public async Task<DispositionExecutionTraceDto?> GetTraceAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances
            .SingleOrDefaultAsync(x => x.Id == workflowInstanceId, cancellationToken);

        if (workflow is null)
        {
            return null;
        }

        var tools = await dbContext.ToolInvocations
            .Where(x => x.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(x => x.StartedAtUtc)
            .Select(x => new ToolInvocationDto(
                x.Id,
                x.ToolName,
                x.Status,
                x.TraceId,
                x.DurationMs,
                x.InputSummary,
                x.OutputSummary,
                x.ErrorMessage))
            .ToListAsync(cancellationToken);

        var checkpoints = await dbContext.WorkflowCheckpoints
            .Where(x => x.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(x => x.Superstep)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new WorkflowCheckpointDto(
                x.Id,
                x.Superstep,
                x.CheckpointType,
                x.StateJson))
            .ToListAsync(cancellationToken);

        return new DispositionExecutionTraceDto(
            workflow.Id,
            workflow.WorkflowCode,
            workflow.Status,
            workflow.ApprovalReferenceId,
            tools,
            checkpoints);
    }
}
