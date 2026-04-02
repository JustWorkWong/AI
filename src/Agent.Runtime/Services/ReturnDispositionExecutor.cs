using System.Text.Json;
using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Tools;
using Agent.Runtime.Workflows;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Services;

public sealed class ReturnDispositionExecutor(
    IDomainKnowledgeClient domainKnowledgeClient,
    IDomainDispositionClient domainDispositionClient,
    ToolLoggingMiddleware toolLoggingMiddleware,
    AgentRuntimeDbContext dbContext)
{
    public async Task<DispositionExecutionResultDto> ExecuteAsync(
        Guid returnOrderId,
        ExecuteDispositionRequest request,
        CancellationToken cancellationToken)
    {
        var workflowInstance = new WorkflowInstance
        {
            SessionId = returnOrderId,
            WorkflowCode = "return-disposition-execute",
            Status = WorkflowInstanceStatus.Running
        };

        dbContext.WorkflowInstances.Add(workflowInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");
        var context = BuildRuntimeContext(workflowInstance.Id, traceId);

        try
        {
            var workflow = new ReturnDispositionWorkflow();
            var result = await workflow.RunAsync(
                new ReturnDispositionInput(returnOrderId, request.IdempotencyKey),
                context,
                cancellationToken);

            await PersistCheckpointsAsync(workflowInstance.Id, context.Checkpoints, cancellationToken);

            workflowInstance.ApprovalReferenceId = result.ApprovalReferenceId;
            workflowInstance.Status = result.Status switch
            {
                "WaitingForApproval" => WorkflowInstanceStatus.WaitingApproval,
                "Completed" => WorkflowInstanceStatus.Completed,
                _ => WorkflowInstanceStatus.Failed
            };
            workflowInstance.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DispositionExecutionResultDto(
                workflowInstance.Id,
                result.Status,
                result.ApprovalReferenceId,
                result.Outcome);
        }
        catch
        {
            workflowInstance.Status = WorkflowInstanceStatus.Failed;
            workflowInstance.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private RuntimeContext BuildRuntimeContext(Guid workflowInstanceId, string traceId)
    {
        var context = new RuntimeContext();

        context.RegisterTool(
            GetReturnOrderTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (GetReturnOrderToolInput)input;
                var order = await toolLoggingMiddleware.ExecuteAsync(
                    GetReturnOrderTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    ct => domainKnowledgeClient.GetReturnOrderAsync(request.ReturnOrderId, ct),
                    workflowInstanceId,
                    cancellationToken)
                    ?? throw new ReturnOrderNotFoundException(request.ReturnOrderId);

                return new ReturnOrderSnapshot(order.ReturnOrderId, order.QualityState);
            });

        context.RegisterTool(
            SearchSopTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (SearchReturnSopToolInput)input;
                var chunks = await toolLoggingMiddleware.ExecuteAsync(
                    SearchSopTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    async ct =>
                    {
                        var candidates = await domainKnowledgeClient.SearchSopCandidatesAsync("RETURNS", "DISPOSITION", ct);
                        return await domainKnowledgeClient.RetrieveSopChunksAsync(
                            new RetrieveSopChunksQuery("RETURNS", "DISPOSITION", candidates.Select(x => x.DocumentId).ToArray()),
                            ct);
                    },
                    workflowInstanceId,
                    cancellationToken);

                return chunks.Select(x => x.Content).ToArray();
            });

        context.RegisterTool(
            SearchHistoricalCasesTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (SearchHistoricalCasesToolInput)input;
                var cases = await toolLoggingMiddleware.ExecuteAsync(
                    SearchHistoricalCasesTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    ct => domainKnowledgeClient.GetHistoricalCasesAsync(request.ReturnOrderId, ct),
                    workflowInstanceId,
                    cancellationToken);

                return cases.Select(x => $"{x.Condition}->{x.Outcome}").ToArray();
            });

        context.RegisterTool(
            RequestDispositionApprovalTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (RequestDispositionApprovalToolInput)input;
                var approvalReferenceId = await toolLoggingMiddleware.ExecuteAsync(
                    RequestDispositionApprovalTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    ct => domainDispositionClient.RequestApprovalAsync(
                        new RequestDispositionApproval(request.ReturnOrderId, request.Outcome),
                        ct),
                    workflowInstanceId,
                    cancellationToken);

                return new ApprovalReference(approvalReferenceId);
            });

        context.RegisterTool(
            ApplyDispositionDecisionTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (ApplyDispositionDecisionToolInput)input;
                await toolLoggingMiddleware.ExecuteAsync(
                    ApplyDispositionDecisionTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    async ct =>
                    {
                        await domainDispositionClient.ApplyDispositionAsync(
                            new ApplyDispositionCommand(request.ReturnOrderId, request.Outcome, request.IdempotencyKey),
                            ct);
                        return "accepted";
                    },
                    workflowInstanceId,
                    cancellationToken);

                return new { Accepted = true };
            });

        context.RegisterGenerator(
            "return-disposition",
            (input, _) =>
            {
                dynamic request = input;
                var returnOrder = (ReturnOrderSnapshot)request.ReturnOrder;
                var requiresApproval = returnOrder.QualityState.Equals("Broken", StringComparison.OrdinalIgnoreCase);
                var outcome = requiresApproval ? "Scrap" : "Resell";

                return Task.FromResult<object?>(new DispositionSuggestion(requiresApproval, outcome));
            });

        return context;
    }

    private async Task PersistCheckpointsAsync(
        Guid workflowInstanceId,
        IReadOnlyList<RuntimeCheckpoint> checkpoints,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < checkpoints.Count; index++)
        {
            var checkpoint = checkpoints[index];
            dbContext.WorkflowCheckpoints.Add(new WorkflowCheckpoint
            {
                WorkflowInstanceId = workflowInstanceId,
                Superstep = index + 1,
                CheckpointType = checkpoint.CheckpointType,
                StateJson = JsonSerializer.Serialize(checkpoint.Value)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
