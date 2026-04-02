using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionApprovalEndpointsTests
{
    [Fact]
    public async Task Post_approval_should_return_completed_runtime_result()
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
                    services.AddSingleton<IAgentRuntimeClient>(new StubAgentRuntimeClient
                    {
                        DecideDispositionApprovalAsyncHandler = static (workflowInstanceId, _, _) => Task.FromResult<DispositionExecutionResultDto?>(new DispositionExecutionResultDto(
                            workflowInstanceId,
                            "Completed",
                            Guid.Parse("77777777-7777-7777-7777-777777777777"),
                            "Scrap"))
                    });
                });
            });

        var client = app.CreateClient();
        var workflowInstanceId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/returns/workbench/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>();
        Assert.NotNull(payload);
        Assert.Equal("Completed", payload!.Status);
    }
}
