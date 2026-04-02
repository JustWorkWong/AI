using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.IntegrationTests;

public sealed class DispositionEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public DispositionEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Apply_disposition_should_ignore_duplicate_idempotency_keys()
    {
        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        var client = app.CreateClient();

        var command = new
        {
            returnOrderId = Guid.NewGuid(),
            idempotencyKey = "cmd-123",
            outcome = "Resell"
        };

        var first = await client.PostAsJsonAsync("/internal/dispositions/apply", command);
        var second = await client.PostAsJsonAsync("/internal/dispositions/apply", command);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        Assert.Equal(1, db.CommandDeduplications.Count(x => x.IdempotencyKey == "cmd-123"));
        Assert.Equal(1, db.DispositionDecisions.Count(x => x.ReturnOrderId == command.returnOrderId));
    }
}
