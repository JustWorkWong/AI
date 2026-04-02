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
        ValidateAction(request);

        var workflow = await LoadWorkflowAsync(workflowInstanceId, cancellationToken);
        var approvalReferenceId = workflow.ApprovalReferenceId
            ?? throw new WorkflowConflictException("Workflow approval reference is missing.");
        var state = await LoadApprovalCheckpointStateAsync(workflowInstanceId, cancellationToken);

        EnsureApprovalReferenceConsistency(workflow, state);
        await ClaimApprovalAsync(workflow, cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");
        var command = new ApprovalDecisionCommand(request.Action, request.Actor);

        await toolLoggingMiddleware.ExecuteAsync(
            "DecideApproval",
            traceId,
            JsonSerializer.Serialize(new { workflowInstanceId, approvalReferenceId, request.Action, request.Actor }),
            async ct =>
            {
                await domainDispositionClient.DecideApprovalAsync(approvalReferenceId, command, ct);
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

            return await CompleteApprovalAsync(
                workflow,
                "Completed",
                approvalReferenceId,
                state.Outcome,
                cancellationToken);
        }

        if (string.Equals(request.Action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            return await CompleteApprovalAsync(
                workflow,
                "Rejected",
                approvalReferenceId,
                null,
                cancellationToken);
        }

        throw new ArgumentException("Unsupported approval action.", nameof(request));
    }

    private static void ValidateAction(ApprovalDecisionRequest request)
    {
        if (!string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unsupported approval action.", nameof(request));
        }
    }

    private async Task<WorkflowInstance> LoadWorkflowAsync(Guid workflowInstanceId, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.WorkflowInstances.SingleOrDefaultAsync(
            x => x.Id == workflowInstanceId,
            cancellationToken)
            ?? throw new WorkflowNotFoundException($"Workflow '{workflowInstanceId}' was not found.");

        if (!string.Equals(workflow.Status, WorkflowInstanceStatus.WaitingApproval, StringComparison.Ordinal))
        {
            throw new WorkflowConflictException("Workflow is not waiting for approval.");
        }

        return workflow;
    }

    private async Task<ApprovalCheckpointState> LoadApprovalCheckpointStateAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var checkpoint = await dbContext.WorkflowCheckpoints
                .Where(x => x.WorkflowInstanceId == workflowInstanceId && x.CheckpointType == "approval")
                .OrderByDescending(x => x.Superstep)
                .ThenByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new WorkflowConflictException("Approval checkpoint was not found.");

            return JsonSerializer.Deserialize<ApprovalCheckpointState>(checkpoint.StateJson, CheckpointJsonOptions)
                ?? throw new WorkflowConflictException("Approval checkpoint state was invalid.");
        }
        catch (JsonException ex)
        {
            throw new WorkflowConflictException("Approval checkpoint state was invalid.", ex);
        }
    }

    private static void EnsureApprovalReferenceConsistency(
        WorkflowInstance workflow,
        ApprovalCheckpointState state)
    {
        if (workflow.ApprovalReferenceId != state.ApprovalReferenceId)
        {
            throw new WorkflowConflictException("Workflow approval reference does not match checkpoint state.");
        }
    }

    private async Task ClaimApprovalAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        workflow.ClaimApproval();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new WorkflowConflictException("Workflow approval is already being processed.", ex);
        }
    }

    private async Task<DispositionExecutionResultDto> CompleteApprovalAsync(
        WorkflowInstance workflow,
        string resultStatus,
        Guid approvalReferenceId,
        string? outcome,
        CancellationToken cancellationToken)
    {
        if (string.Equals(resultStatus, "Completed", StringComparison.Ordinal))
        {
            workflow.CompleteApproval();
        }
        else
        {
            workflow.RejectApproval();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new WorkflowConflictException("Workflow approval could not be finalized.", ex);
        }

        return new DispositionExecutionResultDto(
            workflow.Id,
            resultStatus,
            approvalReferenceId,
            outcome);
    }
}

public sealed class WorkflowNotFoundException : Exception
{
    public WorkflowNotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class WorkflowConflictException : Exception
{
    public WorkflowConflictException(string message)
        : base(message)
    {
    }

    public WorkflowConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
