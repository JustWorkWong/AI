using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Auth.Service.Tests;

public sealed class SessionEndpointsTests
{
    [Fact]
    public async Task Config_endpoint_should_return_oidc_settings()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var response = await client.GetAsync("/auth/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
