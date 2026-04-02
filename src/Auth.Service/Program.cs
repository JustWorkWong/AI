using Auth.Service.Clients;
using Auth.Service.Endpoints;
using Auth.Service.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Wms.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddWmsServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddOptions<KeycloakOptions>()
    .BindConfiguration(KeycloakOptions.SectionName);

var keycloak = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
    ?? new KeycloakOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloak.Authority;
        options.Audience = keycloak.Audience;
        options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<DomainUserSyncClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:WmsDomainService"]
        ?? "http://wms-domain-service");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapSessionEndpoints();
app.MapSyncEndpoints();
app.MapWmsDefaultEndpoints();

app.Run();

public partial class Program;
