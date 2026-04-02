using System.Net.Http.Json;
using Shared.Contracts.Returns;

namespace Agent.Runtime.Clients;

public interface IDomainDispositionClient
{
    Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken);

    Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken);
}

public sealed class DomainDispositionClient(HttpClient httpClient) : IDomainDispositionClient
{
    public async Task<Guid> RequestApprovalAsync(
        RequestDispositionApproval command,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/internal/dispositions/request-approval",
            command,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException("Approval location header was missing.");
        }

        var approvalIdSegment = location.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Guid.Parse(approvalIdSegment);
    }

    public async Task ApplyDispositionAsync(
        ApplyDispositionCommand command,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/internal/dispositions/apply",
            command,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
