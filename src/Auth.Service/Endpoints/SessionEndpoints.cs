using Auth.Service.Options;
using Microsoft.Extensions.Options;

namespace Auth.Service.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/config", (IOptions<KeycloakOptions> options) =>
        {
            var config = options.Value;

            return Results.Ok(new
            {
                authority = config.Authority,
                audience = config.Audience,
                requireHttpsMetadata = config.RequireHttpsMetadata
            });
        });

        return endpoints;
    }
}
