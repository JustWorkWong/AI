using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
using Shared.Contracts.Sop;

namespace Ops.Bff.Tests;

public sealed class DashboardEndpointsTests
{
    [Fact]
    public async Task Get_dashboard_should_return_pending_counts()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDomainServiceClient>();
                    services.RemoveAll<IAgentRuntimeClient>();
                    services.AddSingleton<IDomainServiceClient>(new StubDomainServiceClient());
                    services.AddSingleton<IAgentRuntimeClient>(new StubAgentRuntimeClient());
                });
            });

        var client = app.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class StubDomainServiceClient : IDomainServiceClient
    {
        public Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(3);

        public Task<object?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<object?>(null);
    }

    private sealed class StubAgentRuntimeClient : IAgentRuntimeClient
    {
        public Task<int> GetFailureCountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<object?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<object?>(null);

        public Task<object?> AdvanceSopSessionAsync(
            Guid sessionId,
            AdvanceSopStepRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<object?>(null);

        public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
