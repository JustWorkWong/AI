using System.Net;
using System.Text.Json;
using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Agent.Runtime.Tests;

public sealed class ProgramProblemDetailsTests
{
    [Fact]
    public async Task Approval_endpoint_should_return_422_problem_details_for_invalid_action()
    {
        await using var db = CreateDbContext();
        var service = CreateApprovalService(db);
        var context = CreateContext("trace-422");

        var result = await global::Program.HandleDispositionApproval(
            Guid.NewGuid(),
            context,
            new ApprovalDecisionRequest("Maybe", "manager-1"),
            service,
            CancellationToken.None);

        var response = await ExecuteAsync(result, context);

        Assert.Equal((int)HttpStatusCode.UnprocessableEntity, response.Response.StatusCode);
        Assert.Equal("application/problem+json", response.Response.ContentType);
        Assert.Equal(422, response.Body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("trace-422", response.Body.RootElement.GetProperty("traceId").GetString());
        Assert.False(response.Body.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Approval_endpoint_should_return_404_when_workflow_is_missing()
    {
        await using var db = CreateDbContext();
        var service = CreateApprovalService(db);
        var context = CreateContext("trace-404");

        var result = await global::Program.HandleDispositionApproval(
            Guid.NewGuid(),
            context,
            new ApprovalDecisionRequest("Approve", "manager-1"),
            service,
            CancellationToken.None);

        var response = await ExecuteAsync(result, context);

        Assert.Equal((int)HttpStatusCode.NotFound, response.Response.StatusCode);
        Assert.Equal(404, response.Body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("trace-404", response.Body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Approval_endpoint_should_return_409_when_workflow_is_not_waiting_for_approval()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = Guid.NewGuid();
        db.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowInstanceId,
            SessionId = Guid.NewGuid(),
            WorkflowCode = "return-disposition-execute",
            Status = WorkflowInstanceStatus.Completed,
            ApprovalReferenceId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var service = CreateApprovalService(db);
        var context = CreateContext("trace-409-state");

        var result = await global::Program.HandleDispositionApproval(
            workflowInstanceId,
            context,
            new ApprovalDecisionRequest("Approve", "manager-1"),
            service,
            CancellationToken.None);

        var response = await ExecuteAsync(result, context);

        Assert.Equal((int)HttpStatusCode.Conflict, response.Response.StatusCode);
        Assert.Equal(409, response.Body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("trace-409-state", response.Body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Approval_endpoint_should_return_409_when_approval_reference_is_missing()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = Guid.NewGuid();
        db.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowInstanceId,
            SessionId = Guid.NewGuid(),
            WorkflowCode = "return-disposition-execute",
            Status = WorkflowInstanceStatus.WaitingApproval
        });
        await db.SaveChangesAsync();

        var service = CreateApprovalService(db);
        var context = CreateContext("trace-409-reference");

        var result = await global::Program.HandleDispositionApproval(
            workflowInstanceId,
            context,
            new ApprovalDecisionRequest("Approve", "manager-1"),
            service,
            CancellationToken.None);

        var response = await ExecuteAsync(result, context);

        Assert.Equal((int)HttpStatusCode.Conflict, response.Response.StatusCode);
        Assert.Equal(409, response.Body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("trace-409-reference", response.Body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Approval_endpoint_should_return_409_when_checkpoint_state_is_invalid()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = Guid.NewGuid();
        db.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowInstanceId,
            SessionId = Guid.NewGuid(),
            WorkflowCode = "return-disposition-execute",
            Status = WorkflowInstanceStatus.WaitingApproval,
            ApprovalReferenceId = Guid.NewGuid()
        });
        db.WorkflowCheckpoints.Add(new WorkflowCheckpoint
        {
            WorkflowInstanceId = workflowInstanceId,
            Superstep = 1,
            CheckpointType = "approval",
            StateJson = "{not-valid-json}"
        });
        await db.SaveChangesAsync();

        var service = CreateApprovalService(db);
        var context = CreateContext("trace-409-json");

        var result = await global::Program.HandleDispositionApproval(
            workflowInstanceId,
            context,
            new ApprovalDecisionRequest("Approve", "manager-1"),
            service,
            CancellationToken.None);

        var response = await ExecuteAsync(result, context);

        Assert.Equal((int)HttpStatusCode.Conflict, response.Response.StatusCode);
        Assert.Equal(409, response.Body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("trace-409-json", response.Body.RootElement.GetProperty("traceId").GetString());
    }

    private static AgentRuntimeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentRuntimeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AgentRuntimeDbContext(options);
    }

    private static ReturnDispositionApprovalService CreateApprovalService(AgentRuntimeDbContext db)
    {
        var toolLoggingMiddleware = new ToolLoggingMiddleware(new EfToolInvocationStore(db));
        return new ReturnDispositionApprovalService(
            new StubDomainDispositionClient(),
            toolLoggingMiddleware,
            db);
    }

    private static DefaultHttpContext CreateContext(string traceId)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = traceId;
        context.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        return context;
    }

    private static async Task<(HttpResponse Response, JsonDocument Body)> ExecuteAsync(
        IResult result,
        DefaultHttpContext context)
    {
        await using var stream = new MemoryStream();
        context.Response.Body = stream;

        await result.ExecuteAsync(context);

        stream.Position = 0;
        var body = await JsonDocument.ParseAsync(stream);
        return (context.Response, body);
    }

    private sealed class StubDomainDispositionClient : IDomainDispositionClient
    {
        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
