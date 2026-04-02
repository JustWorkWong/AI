using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Approvals;
using Wms.DomainService.Approvals;
using Wms.DomainService.Persistence;
using System.Text.Json;

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

    [Fact]
    public async Task Post_invalid_approval_action_should_return_problem_details_with_trace_id()
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
            new ApprovalDecisionRequest("Maybe", "manager-1"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(422, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
