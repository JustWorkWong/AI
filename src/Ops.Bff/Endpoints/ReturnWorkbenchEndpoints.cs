using Ops.Bff.Clients;
using Ops.Bff.Queries;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Ops.Bff.Endpoints;

public static class ReturnWorkbenchEndpoints
{
    public static IEndpointRouteBuilder MapReturnWorkbenchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/returns/workbench/{returnOrderId:guid}", async (
            Guid returnOrderId,
            IReturnWorkbenchQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var view = await queryService.GetViewAsync(returnOrderId, cancellationToken);
            return view is null ? Results.NotFound() : Results.Ok(view);
        });

        endpoints.MapPost("/api/returns/workbench/{returnOrderId:guid}/execute", async (
            Guid returnOrderId,
            ExecuteDispositionRequest request,
            IAgentRuntimeClient runtimeClient,
            CancellationToken cancellationToken) =>
        {
            var result = await runtimeClient.ExecuteDispositionAsync(returnOrderId, request, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        endpoints.MapGet("/api/returns/workbench/executions/{workflowInstanceId:guid}", async (
            Guid workflowInstanceId,
            IAgentRuntimeClient runtimeClient,
            CancellationToken cancellationToken) =>
        {
            var result = await runtimeClient.GetDispositionTraceAsync(workflowInstanceId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        endpoints.MapPost("/api/returns/workbench/executions/{workflowInstanceId:guid}/approval", async (
            Guid workflowInstanceId,
            ApprovalDecisionRequest request,
            IAgentRuntimeClient runtimeClient,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await runtimeClient.DecideDispositionApprovalAsync(
                    workflowInstanceId,
                    request,
                    cancellationToken);

                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return endpoints;
    }
}
