using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionExecutionEndpointsTests
{
    [Fact]
    public async Task Post_execute_should_return_runtime_execution_result()
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
                        ExecuteDispositionAsyncHandler = static (_, _, _) => Task.FromResult<DispositionExecutionResultDto?>(new DispositionExecutionResultDto(
                            Guid.NewGuid(),
                            "Completed",
                            null,
                            "Resell"))
                    });
                });
            });

        var client = app.CreateClient();
        var returnOrderId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/returns/workbench/{returnOrderId}/execute",
            new ExecuteDispositionRequest("idem-bff"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>();
        Assert.NotNull(payload);
        Assert.Equal("Completed", payload!.Status);
        Assert.Equal("Resell", payload.Outcome);
    }
}
