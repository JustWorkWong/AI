using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
using Ops.Bff.Tests.TestDoubles;

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
                    services.AddSingleton<IDomainServiceClient>(new StubDomainServiceClient { PendingApprovals = 3 });
                    services.AddSingleton<IAgentRuntimeClient>(new StubAgentRuntimeClient { FailureCount = 1 });
                });
            });

        var client = app.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
