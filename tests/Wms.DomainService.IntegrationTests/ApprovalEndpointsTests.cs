using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Approvals;
using Wms.DomainService.Approvals;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.IntegrationTests;

public sealed class ApprovalEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ApprovalEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Post_approval_action_should_mark_task_approved_and_store_action()
    {
        var approvalTaskId = Guid.NewGuid();

        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            db.ApprovalTasks.Add(new ApprovalTask(approvalTaskId, "Disposition", Guid.NewGuid()));
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/internal/approvals/{approvalTaskId}/actions",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<WmsDbContext>();
        var task = await verifyDb.ApprovalTasks.FindAsync(approvalTaskId);

        Assert.NotNull(task);
        Assert.Equal("Approved", task!.Status);
        Assert.Single(verifyDb.ApprovalActions.Where(x => x.ApprovalTaskId == approvalTaskId));
    }
}
