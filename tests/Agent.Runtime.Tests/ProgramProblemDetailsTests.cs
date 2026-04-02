using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Agent.Runtime.Clients;
using Agent.Runtime.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

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
    public async Task Approval_endpoint_should_return_409_problem_details_when_approval_reference_is_inconsistent()
    {
        var workflowInstanceId = Guid.NewGuid();
        var workflowApprovalReferenceId = Guid.NewGuid();
        var checkpointApprovalReferenceId = Guid.NewGuid();

        using var app = await CreateAppAsync(db =>
        {
            db.WorkflowInstances.Add(new WorkflowInstance
            {
                Id = workflowInstanceId,
                SessionId = Guid.NewGuid(),
                WorkflowCode = "return-disposition-execute",
                Status = WorkflowInstanceStatus.WaitingApproval,
                ApprovalReferenceId = workflowApprovalReferenceId
            });

            db.WorkflowCheckpoints.Add(new WorkflowCheckpoint
            {
                WorkflowInstanceId = workflowInstanceId,
                Superstep = 1,
                CheckpointType = "approval",
                StateJson =
                    $$"""
                    {"approvalReferenceId":"{{checkpointApprovalReferenceId}}","returnOrderId":"{{Guid.NewGuid()}}","outcome":"Scrap","idempotencyKey":"idem-approval"}
                    """
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
    public async Task Approval_endpoint_should_not_double_send_side_effects_when_two_approve_requests_race()
    {
        var workflowInstanceId = Guid.NewGuid();
        var approvalReferenceId = Guid.NewGuid();
        var dispositionClient = new BlockingDispositionClient();

        using var app = await CreateAppAsync(
            db =>
            {
                db.WorkflowInstances.Add(new WorkflowInstance
                {
                    Id = workflowInstanceId,
                    SessionId = Guid.NewGuid(),
                    WorkflowCode = "return-disposition-execute",
                    Status = WorkflowInstanceStatus.WaitingApproval,
                    ApprovalReferenceId = approvalReferenceId
                });

                db.WorkflowCheckpoints.Add(new WorkflowCheckpoint
                {
                    WorkflowInstanceId = workflowInstanceId,
                    Superstep = 1,
                    CheckpointType = "approval",
                    StateJson =
                        $$"""
                        {"approvalReferenceId":"{{approvalReferenceId}}","returnOrderId":"{{Guid.NewGuid()}}","outcome":"Scrap","idempotencyKey":"idem-approval"}
                        """
                });
            },
            services =>
            {
                services.RemoveAll<IDomainDispositionClient>();
                services.AddSingleton<IDomainDispositionClient>(dispositionClient);
            });

        var client = app.CreateClient();

        var firstRequest = client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        await dispositionClient.DecideEntered.Task;

        var secondRequest = client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-2"));

        var secondResponse = await secondRequest;
        dispositionClient.ReleaseDecide();
        var firstResponse = await firstRequest;

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.Equal(1, dispositionClient.DecideCallCount);
        Assert.Equal(1, dispositionClient.ApplyCallCount);
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
    public async Task Disposition_execute_endpoint_should_return_500_problem_details_when_location_header_is_missing()
    {
        var dispositionClient = new ThrowingDispositionClient();
        var knowledgeClient = new HighRiskDomainKnowledgeClient();

        using var app = await CreateAppAsync(
            configureServices: services =>
            {
                services.RemoveAll<IDomainKnowledgeClient>();
                services.RemoveAll<IDomainDispositionClient>();
                services.AddSingleton<IDomainKnowledgeClient>(knowledgeClient);
                services.AddSingleton<IDomainDispositionClient>(dispositionClient);
            });

        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/internal/runtime/dispositions/{Guid.NewGuid()}/execute",
            new ExecuteDispositionRequest("idem-fault"));

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, 500);
    }

    [Fact]
    public async Task Runtime_initializer_should_add_version_column_idempotently()
    {
        var connectionString = await CreateIsolatedDatabaseAsync(_fixture.ConnectionString, "agent_runtime");
        using var app = await CreateAppAsync(connectionString: connectionString);
        var workflowInstanceId = Guid.NewGuid();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgentRuntimeDbContext>();
            db.WorkflowInstances.Add(new WorkflowInstance
            {
                Id = workflowInstanceId,
                SessionId = Guid.NewGuid(),
                WorkflowCode = "return-disposition-execute",
                Status = WorkflowInstanceStatus.WaitingApproval,
                ApprovalReferenceId = Guid.NewGuid()
            });
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE agent_runtime.workflow_instances DROP COLUMN IF EXISTS "Version";
                """);
        }

        var development = new StubHostEnvironment(Environments.Development);
        await RuntimeDatabaseInitializer.InitializeAsync(app.Services, development);
        await RuntimeDatabaseInitializer.InitializeAsync(app.Services, development);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var valueCommand = new NpgsqlCommand(
            """
            SELECT "Version"
            FROM agent_runtime.workflow_instances
            WHERE "Id" = @id;
            """,
            connection);
        valueCommand.Parameters.AddWithValue("id", workflowInstanceId);

        var version = (int)(await valueCommand.ExecuteScalarAsync() ?? -1);
        Assert.Equal(0, version);
    }

    [Fact]
    public async Task Test_fault_endpoint_should_return_problem_details_with_trace_id()
    {
        using var app = await CreateAppAsync();
        var client = app.CreateClient();

        var response = await client.GetAsync("/internal/test/fault");

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, 500);
    }

    private async Task<WebApplicationFactory<global::Program>> CreateAppAsync(
        Action<AgentRuntimeDbContext>? seed = null,
        Action<IServiceCollection>? configureServices = null,
        string? connectionString = null)
    {
        var app = new RuntimeApiFactory(connectionString ?? _fixture.ConnectionString).WithWebHostBuilder(builder =>
        {
            if (configureServices is not null)
            {
                builder.ConfigureServices(configureServices);
            }
        });

        await EnsureDatabaseAsync(app, seed);
        return app;
    }

    private static async Task EnsureDatabaseAsync(
        WebApplicationFactory<global::Program> app,
        Action<AgentRuntimeDbContext>? seed)
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

    private static async Task<string> CreateIsolatedDatabaseAsync(string connectionString, string prefix)
    {
        var databaseName = $"{prefix}_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName };
        var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"""CREATE DATABASE "{databaseName}";""", connection);
        await command.ExecuteNonQueryAsync();

        return builder.ConnectionString;
    }

    private sealed class BlockingDispositionClient : IDomainDispositionClient
    {
        private int _decideCallCount;
        private int _applyCallCount;
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DecideCallCount => Volatile.Read(ref _decideCallCount);

        public int ApplyCallCount => Volatile.Read(ref _applyCallCount);

        public TaskCompletionSource DecideEntered => _entered;

        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public async Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _decideCallCount);
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _applyCallCount);
            return Task.CompletedTask;
        }

        public void ReleaseDecide() => _release.TrySetResult();
    }

    private sealed class ThrowingDispositionClient : IDomainDispositionClient
    {
        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Approval location header was missing.");

        public Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class HighRiskDomainKnowledgeClient : IDomainKnowledgeClient
    {
        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(new ReturnOrderDto(returnOrderId, "RET-001", "Broken", "Open", "notes"));

        public Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalCaseDto>>([
                new HistoricalCaseDto(Guid.NewGuid(), "Broken", "Scrap")
            ]);

        public Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(
            string operationCode,
            string stepCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopCandidateDto>>([
                new SopCandidateDto(Guid.NewGuid(), "SOP-RET-001", "v1", "guidance")
            ]);

        public Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(
            RetrieveSopChunksQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopChunkDto>>([
                new SopChunkDto(Guid.NewGuid(), Guid.NewGuid(), "SOP-RET-001", "v1", query.StepCode, "guidance")
            ]);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Agent.Runtime.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
