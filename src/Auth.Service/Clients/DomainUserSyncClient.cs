using System.Net.Http.Json;
using System.Security.Claims;
using Shared.Contracts.Approvals;

namespace Auth.Service.Clients;

public sealed class DomainUserSyncClient(HttpClient httpClient)
{
    public async Task SyncAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var payload = new SyncUserRequest(
            user.FindFirstValue("sub") ?? string.Empty,
            user.FindFirstValue("preferred_username") ?? string.Empty,
            user.FindFirstValue("name") ?? string.Empty);

        await httpClient.PostAsJsonAsync("/internal/auth/sync", payload, cancellationToken);
    }
}
