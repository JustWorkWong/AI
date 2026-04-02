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
    public void ClaimApproval_should_move_to_approving_and_increment_version()
    {
        var workflow = new WorkflowInstance
        {
            Status = WorkflowInstanceStatus.WaitingApproval,
            Version = 2
        };

        workflow.ClaimApproval();

        Assert.Equal(WorkflowInstanceStatus.Approving, workflow.Status);
        Assert.Equal(3, workflow.Version);
        Assert.Null(workflow.CompletedAtUtc);
    }

    [Fact]
    public void CompleteApproval_should_mark_completed_and_increment_version()
    {
        var workflow = new WorkflowInstance
        {
            Status = WorkflowInstanceStatus.Approving,
            Version = 1
        };

        workflow.CompleteApproval();

        Assert.Equal(WorkflowInstanceStatus.Completed, workflow.Status);
        Assert.Equal(2, workflow.Version);
        Assert.NotNull(workflow.CompletedAtUtc);
    }

    [Fact]
    public void RejectApproval_should_mark_rejected_and_increment_version()
    {
        var workflow = new WorkflowInstance
        {
            Status = WorkflowInstanceStatus.Approving,
            Version = 4
        };

        workflow.RejectApproval();

        Assert.Equal(WorkflowInstanceStatus.Rejected, workflow.Status);
        Assert.Equal(5, workflow.Version);
        Assert.NotNull(workflow.CompletedAtUtc);
    }

    [Fact]
    public void Fail_should_mark_failed_and_increment_version()
    {
        var workflow = new WorkflowInstance
        {
            Status = WorkflowInstanceStatus.Running,
            Version = 7
        };

        workflow.Fail();

        Assert.Equal(WorkflowInstanceStatus.Failed, workflow.Status);
        Assert.Equal(8, workflow.Version);
        Assert.NotNull(workflow.CompletedAtUtc);
    }

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
        Assert.Equal(2, workflow.Version);
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
        Assert.Equal(2, workflow.Version);
    }

    [Fact]
    public async Task Decide_failure_after_claim_should_restore_waiting_approval()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = await SeedWaitingWorkflowAsync(db);
        var client = new FailingDomainDispositionClient
        {
            DecideFailuresRemaining = 1
        };
        var service = new ReturnDispositionApprovalService(
            client,
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DecideAsync(
                workflowInstanceId,
                new ApprovalDecisionRequest("Reject", "manager-3"),
                CancellationToken.None));

        var workflow = await db.WorkflowInstances.SingleAsync(x => x.Id == workflowInstanceId);
        Assert.Equal(WorkflowInstanceStatus.WaitingApproval, workflow.Status);
        Assert.Equal(2, workflow.Version);
        Assert.Null(workflow.CompletedAtUtc);
    }

    [Fact]
    public async Task Apply_failure_after_approval_should_mark_workflow_failed_and_block_retry()
    {
        await using var db = CreateDbContext();
        var workflowInstanceId = await SeedWaitingWorkflowAsync(db);
        var client = new FailingDomainDispositionClient
        {
            ApplyFailuresRemaining = 1
        };
        var service = new ReturnDispositionApprovalService(
            client,
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DecideAsync(
                workflowInstanceId,
                new ApprovalDecisionRequest("Approve", "manager-4"),
                CancellationToken.None));

        var afterFailure = await db.WorkflowInstances.SingleAsync(x => x.Id == workflowInstanceId);
        Assert.Equal(WorkflowInstanceStatus.Failed, afterFailure.Status);
        Assert.Equal(2, afterFailure.Version);
        Assert.NotNull(afterFailure.CompletedAtUtc);

        await Assert.ThrowsAsync<WorkflowConflictException>(() =>
            service.DecideAsync(
                workflowInstanceId,
                new ApprovalDecisionRequest("Approve", "manager-4"),
                CancellationToken.None));

        Assert.Single(client.Decisions);
        Assert.Single(client.AppliedCommands);
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

    private sealed class FailingDomainDispositionClient : IDomainDispositionClient
    {
        public int DecideFailuresRemaining { get; set; }
        public int ApplyFailuresRemaining { get; set; }
        public List<ApplyDispositionCommand> AppliedCommands { get; } = [];
        public List<ApprovalDecisionCommand> Decisions { get; } = [];

        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken)
        {
            AppliedCommands.Add(command);
            if (ApplyFailuresRemaining > 0)
            {
                ApplyFailuresRemaining--;
                throw new InvalidOperationException("apply failed");
            }

            return Task.CompletedTask;
        }

        public Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken)
        {
            Decisions.Add(command);
            if (DecideFailuresRemaining > 0)
            {
                DecideFailuresRemaining--;
                throw new InvalidOperationException("decide failed");
            }

            return Task.CompletedTask;
        }
    }
}
