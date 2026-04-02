using Ops.Bff.Clients;

namespace Ops.Bff.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", async (
            IDomainServiceClient domainClient,
            IAgentRuntimeClient runtimeClient,
            CancellationToken cancellationToken) =>
        {
            var pendingApprovals = await domainClient.GetPendingApprovalsAsync(cancellationToken);
            var workflowFailures = await runtimeClient.GetFailureCountAsync(cancellationToken);

            return Results.Ok(new
            {
                pendingApprovals,
                workflowFailures
            });
        });

        return endpoints;
    }
}
