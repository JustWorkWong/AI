using Ops.Bff.Clients;
using Ops.Bff.Endpoints;
using Ops.Bff.Queries;
using Wms.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddWmsServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IDomainServiceClient, DomainServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:WmsDomainService"]
        ?? "http://wms-domain-service");
});

builder.Services.AddHttpClient<IAgentRuntimeClient, AgentRuntimeClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:AgentRuntime"]
        ?? "http://agent-runtime");
});
builder.Services.AddScoped<IReturnWorkbenchQueryService, ReturnWorkbenchQueryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDashboardEndpoints();
app.MapReturnWorkbenchEndpoints();
app.MapSopAssistEndpoints();
app.MapWmsDefaultEndpoints();

app.Run();

public partial class Program;
