using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Returns;
using Wms.DomainService.Persistence;
using Wms.DomainService.Returns;
using System.Text.Json;

namespace Wms.DomainService.IntegrationTests;

public sealed class ReturnReadEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ReturnReadEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_return_order_should_project_latest_quality_state_and_notes()
    {
        var returnOrderId = Guid.NewGuid();

        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            db.ReturnOrders.Add(new ReturnOrder(returnOrderId, "RET-0001"));
            db.QualityInspections.Add(new QualityInspection(Guid.NewGuid(), returnOrderId, "BoxDamaged", "Outer box dented"));
            db.QualityInspections.Add(new QualityInspection(Guid.NewGuid(), returnOrderId, "Broken", "Screen cracked"));
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();

        var response = await client.GetAsync($"/internal/returns/{returnOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ReturnOrderDto>();
        Assert.NotNull(payload);
        Assert.Equal(returnOrderId, payload!.ReturnOrderId);
        Assert.Equal("Broken", payload.QualityState);
        Assert.Equal("Screen cracked", payload.Notes);
    }

    [Fact]
    public async Task Get_missing_return_order_should_return_problem_details_with_trace_id()
    {
        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        var client = app.CreateClient();
        var returnOrderId = Guid.NewGuid();

        var response = await client.GetAsync($"/internal/returns/{returnOrderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Get_historical_cases_should_return_matches_for_latest_quality_state()
    {
        var returnOrderId = Guid.NewGuid();

        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            db.ReturnOrders.Add(new ReturnOrder(returnOrderId, "RET-0002"));
            db.QualityInspections.Add(new QualityInspection(Guid.NewGuid(), returnOrderId, "Broken", "Back cover cracked"));
            db.HistoricalCaseViews.Add(new HistoricalCaseView(Guid.NewGuid(), "Broken", "Scrap"));
            db.HistoricalCaseViews.Add(new HistoricalCaseView(Guid.NewGuid(), "Sealed", "Resell"));
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();

        var response = await client.GetAsync($"/internal/returns/{returnOrderId}/historical-cases");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<HistoricalCaseDto>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("Broken", payload[0].Condition);
        Assert.Equal("Scrap", payload[0].Outcome);
    }
}
