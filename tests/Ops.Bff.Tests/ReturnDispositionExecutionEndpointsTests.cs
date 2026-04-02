using System.Net;
using System.Net.Http.Json;
using Ops.Bff.Tests.TestHost;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionExecutionEndpointsTests
{
    [Fact]
    public async Task Post_execute_should_return_runtime_execution_result()
    {
        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient(),
            new StubAgentRuntimeClient
            {
                ExecuteDispositionAsyncHandler = static (_, _, _) => Task.FromResult<DispositionExecutionResultDto?>(new DispositionExecutionResultDto(
                    Guid.NewGuid(),
                    "Completed",
                    null,
                    "Resell"))
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
