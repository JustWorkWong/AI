using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Services;
using Agent.Runtime.Workflows;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Agent.Runtime.Tests;

public sealed class ReturnDispositionApprovalServiceTests
{
    [Fact]
    public async Task Approve_should_apply_disposition_and_complete_workflow()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = await SeedWaitingWorkflowAsync(db);
        var client = new StubDomainDispositionClient();
        var service = new ReturnDispositionApprovalService(
            client,
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        var result = await service.DecideAsync(
            workflowInstanceId,
            new ApprovalDecisionRequest("Approve", "manager-1"),
            CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("Scrap", result.Outcome);
        Assert.Single(client.AppliedCommands);

        var workflow = await db.WorkflowInstances.SingleAsync(x => x.Id == workflowInstanceId);
        Assert.Equal(WorkflowInstanceStatus.Completed, workflow.Status);
    }

    [Fact]
    public async Task Reject_should_mark_workflow_rejected_without_applying_disposition()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = await SeedWaitingWorkflowAsync(db);
        var client = new StubDomainDispositionClient();
        var service = new ReturnDispositionApprovalService(
            client,
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        var result = await service.DecideAsync(
            workflowInstanceId,
            new ApprovalDecisionRequest("Reject", "manager-2"),
            CancellationToken.None);

        Assert.Equal("Rejected", result.Status);
        Assert.Empty(client.AppliedCommands);

        var workflow = await db.WorkflowInstances.SingleAsync(x => x.Id == workflowInstanceId);
        Assert.Equal("Rejected", workflow.Status);
    }

    private static AgentRuntimeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentRuntimeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AgentRuntimeDbContext(options);
    }

    private static async Task<Guid> SeedWaitingWorkflowAsync(AgentRuntimeDbContext db)
    {
        var workflowInstanceId = Guid.NewGuid();
        var approvalReferenceId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        db.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowInstanceId,
            SessionId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            WorkflowCode = "return-disposition-execute",
            Status = WorkflowInstanceStatus.WaitingApproval,
            ApprovalReferenceId = approvalReferenceId
        });

        db.WorkflowCheckpoints.Add(new WorkflowCheckpoint
        {
            WorkflowInstanceId = workflowInstanceId,
            Superstep = 1,
            CheckpointType = "approval",
            StateJson = "{\"approvalReferenceId\":\"55555555-5555-5555-5555-555555555555\",\"returnOrderId\":\"66666666-6666-6666-6666-666666666666\",\"outcome\":\"Scrap\",\"idempotencyKey\":\"idem-approve\"}"
        });

        await db.SaveChangesAsync();
        return workflowInstanceId;
    }

    private sealed class StubDomainDispositionClient : IDomainDispositionClient
    {
        public List<ApplyDispositionCommand> AppliedCommands { get; } = [];
        public List<ApprovalDecisionCommand> Decisions { get; } = [];

        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken)
        {
            AppliedCommands.Add(command);
            return Task.CompletedTask;
        }

        public Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken)
        {
            Decisions.Add(command);
            return Task.CompletedTask;
        }
    }
}
