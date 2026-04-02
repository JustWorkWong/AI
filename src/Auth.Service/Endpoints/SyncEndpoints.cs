using System.Security.Claims;
using Auth.Service.Clients;

namespace Auth.Service.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/sync", async (
            ClaimsPrincipal user,
            DomainUserSyncClient syncClient,
            CancellationToken cancellationToken) =>
        {
            await syncClient.SyncAsync(user, cancellationToken);
            return Results.Accepted();
        }).RequireAuthorization();

        return endpoints;
    }
}
