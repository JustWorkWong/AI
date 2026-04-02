using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.IntegrationTests;

public sealed class AuthProjectionTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public AuthProjectionTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sync_should_upsert_user_projection()
    {
        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/internal/auth/sync", new
        {
            externalSubject = "kc-123",
            userName = "inspector.a",
            displayName = "Inspector A"
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        var user = db.Users.Single(x => x.ExternalSubject == "kc-123");

        Assert.Equal("inspector.a", user.UserName);
        Assert.Equal("Inspector A", user.DisplayName);
    }
}
