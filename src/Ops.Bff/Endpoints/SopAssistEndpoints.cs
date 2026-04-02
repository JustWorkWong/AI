using Ops.Bff.Clients;
using Shared.Contracts.Sop;

namespace Ops.Bff.Endpoints;

public static class SopAssistEndpoints
{
    public static IEndpointRouteBuilder MapSopAssistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/sop/sessions/{sessionId:guid}/steps", async (
            Guid sessionId,
            AdvanceSopStepRequest request,
            IAgentRuntimeClient runtimeClient,
            CancellationToken cancellationToken) =>
        {
            var result = await runtimeClient.AdvanceSopSessionAsync(sessionId, request, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        endpoints.MapGet("/api/sop/sessions/{sessionId:guid}/events", async (
            Guid sessionId,
            HttpContext httpContext,
            IAgentRuntimeClient runtimeClient,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.ContentType = "text/event-stream";
            await runtimeClient.ProxySseAsync(sessionId, httpContext.Response, cancellationToken);
        });

        return endpoints;
    }
}
