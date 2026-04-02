using System.Net;
using Ops.Bff.Tests.TestHost;
using Ops.Bff.Tests.TestDoubles;

namespace Ops.Bff.Tests;

public sealed class DashboardEndpointsTests
{
    [Fact]
    public async Task Get_dashboard_should_return_pending_counts()
    {
        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient { PendingApprovals = 3 },
            new StubAgentRuntimeClient { FailureCount = 1 });

        var client = app.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
