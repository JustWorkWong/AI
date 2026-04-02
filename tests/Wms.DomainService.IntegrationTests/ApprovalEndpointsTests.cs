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
    public async Task Get_missing_approval_task_should_return_problem_details_with_trace_id()
    {
        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        var client = app.CreateClient();
        var approvalTaskId = Guid.NewGuid();

        var response = await client.GetAsync($"/internal/approvals/{approvalTaskId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task Post_approval_action_should_return_conflict_when_task_is_not_pending()
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
        var firstResponse = await client.PostAsJsonAsync(
            $"/internal/approvals/{approvalTaskId}/actions",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            $"/internal/approvals/{approvalTaskId}/actions",
            new ApprovalDecisionRequest("Approve", "manager-2"));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.Equal("application/problem+json", secondResponse.Content.Headers.ContentType?.MediaType);

        var json = await secondResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(409, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));

        await using var verifyScope = app.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<WmsDbContext>();
        Assert.Single(verifyDb.ApprovalActions.Where(x => x.ApprovalTaskId == approvalTaskId));
    }

    [Fact]
    public async Task Concurrent_approval_actions_should_only_persist_one_action()
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
        var approveTask = client.PostAsJsonAsync(
            $"/internal/approvals/{approvalTaskId}/actions",
            new ApprovalDecisionRequest("Approve", "manager-1"));
        var rejectTask = client.PostAsJsonAsync(
            $"/internal/approvals/{approvalTaskId}/actions",
            new ApprovalDecisionRequest("Reject", "manager-2"));

        var responses = await Task.WhenAll(approveTask, rejectTask);

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Accepted);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<WmsDbContext>();
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
