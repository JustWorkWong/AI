using System.Text.Json;
using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Workflows;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Agent.Runtime.Services;

public sealed class ReturnDispositionApprovalService(
    IDomainDispositionClient domainDispositionClient,
    ToolLoggingMiddleware toolLoggingMiddleware,
    AgentRuntimeDbContext dbContext)
{
    private static readonly JsonSerializerOptions CheckpointJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<DispositionExecutionResultDto> DecideAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unsupported approval action.", nameof(request));
        }

        var workflow = await dbContext.WorkflowInstances.SingleOrDefaultAsync(
            x => x.Id == workflowInstanceId,
            cancellationToken)
            ?? throw new InvalidOperationException($"Workflow '{workflowInstanceId}' was not found.");

        if (!string.Equals(workflow.Status, WorkflowInstanceStatus.WaitingApproval, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Workflow is not waiting for approval.");
        }

        if (workflow.ApprovalReferenceId is null)
        {
            throw new InvalidOperationException("Workflow approval reference is missing.");
        }

        var checkpoint = await dbContext.WorkflowCheckpoints
            .Where(x => x.WorkflowInstanceId == workflowInstanceId && x.CheckpointType == "approval")
            .OrderByDescending(x => x.Superstep)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Approval checkpoint was not found.");

        var state = JsonSerializer.Deserialize<ApprovalCheckpointState>(checkpoint.StateJson, CheckpointJsonOptions)
            ?? throw new InvalidOperationException("Approval checkpoint state was invalid.");

        var traceId = Guid.NewGuid().ToString("N");
        var command = new ApprovalDecisionCommand(request.Action, request.Actor);

        await toolLoggingMiddleware.ExecuteAsync(
            "DecideApproval",
            traceId,
            JsonSerializer.Serialize(new { workflowInstanceId, state.ApprovalReferenceId, request.Action, request.Actor }),
            async ct =>
            {
                await domainDispositionClient.DecideApprovalAsync(state.ApprovalReferenceId, command, ct);
                return "accepted";
            },
            workflowInstanceId,
            cancellationToken);

        if (string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase))
        {
            await toolLoggingMiddleware.ExecuteAsync(
                "ApplyDispositionAfterApproval",
                traceId,
                JsonSerializer.Serialize(new { state.ReturnOrderId, state.Outcome, state.IdempotencyKey }),
                async ct =>
                {
                    await domainDispositionClient.ApplyDispositionAsync(
                        new ApplyDispositionCommand(state.ReturnOrderId, state.Outcome, state.IdempotencyKey),
                        ct);
                    return "accepted";
                },
                workflowInstanceId,
                cancellationToken);

            workflow.Status = WorkflowInstanceStatus.Completed;
            workflow.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DispositionExecutionResultDto(
                workflowInstanceId,
                "Completed",
                state.ApprovalReferenceId,
                state.Outcome);
        }

        if (string.Equals(request.Action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            workflow.Status = WorkflowInstanceStatus.Rejected;
            workflow.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DispositionExecutionResultDto(
                workflowInstanceId,
                "Rejected",
                state.ApprovalReferenceId,
                null);
        }
        throw new ArgumentException("Unsupported approval action.", nameof(request));
    }
}
