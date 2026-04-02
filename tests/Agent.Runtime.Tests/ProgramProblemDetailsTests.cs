using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Agent.Runtime.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Agent.Runtime.Tests;

public sealed class ProgramProblemDetailsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ProgramProblemDetailsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Approval_endpoint_should_return_404_problem_details_for_missing_workflow()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{Guid.NewGuid()}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        await AssertProblemAsync(response, HttpStatusCode.NotFound, 404);
    }

    [Fact]
    public async Task Approval_endpoint_should_return_409_problem_details_for_completed_workflow()
    {
        var workflowInstanceId = Guid.NewGuid();
        using var app = await CreateAppAsync(db =>
        {
            db.WorkflowInstances.Add(new WorkflowInstance
            {
                Id = workflowInstanceId,
                SessionId = Guid.NewGuid(),
                WorkflowCode = "return-disposition-execute",
                Status = WorkflowInstanceStatus.Completed,
                ApprovalReferenceId = Guid.NewGuid()
            });
        });

        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, 409);
    }

    [Fact]
    public async Task Approval_endpoint_should_return_409_problem_details_when_approval_reference_is_missing()
    {
        var workflowInstanceId = Guid.NewGuid();
        using var app = await CreateAppAsync(db =>
        {
            db.WorkflowInstances.Add(new WorkflowInstance
            {
                Id = workflowInstanceId,
                SessionId = Guid.NewGuid(),
                WorkflowCode = "return-disposition-execute",
                Status = WorkflowInstanceStatus.WaitingApproval
            });
        });

        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, 409);
    }

    [Fact]
    public async Task Approval_endpoint_should_return_422_problem_details_for_invalid_action()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{Guid.NewGuid()}/approval",
            new ApprovalDecisionRequest("Maybe", "manager-1"));

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, 422);
    }

    [Fact]
    public async Task Disposition_suggestion_endpoint_should_return_404_problem_details_for_missing_return()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();
        var missingReturnId = Guid.NewGuid();

        var response = await client.GetAsync($"/internal/runtime/dispositions/{missingReturnId}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, 404);
    }

    [Fact]
    public async Task Disposition_execute_endpoint_should_return_404_problem_details_for_missing_return()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();
        var missingReturnId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/{missingReturnId}/execute",
            new ExecuteDispositionRequest("idem-missing"));

        await AssertProblemAsync(response, HttpStatusCode.NotFound, 404);
    }

    [Fact]
    public async Task Disposition_trace_endpoint_should_return_404_problem_details_for_missing_workflow()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();
        var missingWorkflowId = Guid.NewGuid();

        var response = await client.GetAsync($"/internal/runtime/dispositions/executions/{missingWorkflowId}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, 404);
    }

    [Fact]
    public async Task Test_fault_endpoint_should_return_problem_details_with_trace_id()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync("/internal/test/fault");

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, 500);
    }

    private async Task<RuntimeApiFactory> CreateAppAsync(Action<AgentRuntimeDbContext>? seed = null)
    {
        var app = new RuntimeApiFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(app, seed);
        return app;
    }

    private static async Task EnsureDatabaseAsync(RuntimeApiFactory app, Action<AgentRuntimeDbContext>? seed)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentRuntimeDbContext>();

        await db.Database.EnsureCreatedAsync();
        seed?.Invoke(db);
        await db.SaveChangesAsync();
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        int expectedProblemStatus)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal(expectedProblemStatus, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
