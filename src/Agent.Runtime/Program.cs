using Microsoft.EntityFrameworkCore;
using Agent.Runtime.Models;
using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Services;
using Agent.Runtime.Streaming;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;
using Wms.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddWmsServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AgentRuntimeDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("aidb")
        ?? builder.Configuration["ConnectionStrings:aidb"]
        ?? "Host=localhost;Database=aidb;Username=postgres;Password=postgres";

    options.UseNpgsql(connectionString);
});

builder.Services.AddHealthChecks();
builder.Services.AddHttpClient<IDomainKnowledgeClient, DomainKnowledgeClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:WmsDomainService"]
        ?? "http://wms-domain-service");
});
builder.Services.AddHttpClient<IDomainDispositionClient, DomainDispositionClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:WmsDomainService"]
        ?? "http://wms-domain-service");
});
builder.Services.AddScoped<IModelGateway, ModelGateway>();
builder.Services.AddScoped<IToolInvocationStore, EfToolInvocationStore>();
builder.Services.AddSingleton<ConversationCompactor>();
builder.Services.AddScoped<ToolLoggingMiddleware>();
builder.Services.AddSingleton<SseEventWriter>();
builder.Services.AddScoped<ReturnDispositionAdvisor>();
builder.Services.AddScoped<ReturnDispositionExecutor>();
builder.Services.AddScoped<SopAssistService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/healthz", () => Results.Ok("ok"));

app.MapGet("/internal/runtime/failures/count", async (
    AgentRuntimeDbContext db,
    CancellationToken cancellationToken) =>
{
    var count = await db.ToolInvocations
        .CountAsync(x => x.Status == ToolInvocationStatus.Failed, cancellationToken);

    return Results.Ok(new RuntimeFailureCountResponse(count));
});

app.MapGet("/internal/runtime/model-profiles/{profileCode}", (
    string profileCode,
    IModelGateway gateway) =>
{
    var profile = gateway.GetProfile(profileCode);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

app.MapGet("/internal/runtime/dispositions/{returnOrderId:guid}", async (
    Guid returnOrderId,
    ReturnDispositionAdvisor advisor,
    CancellationToken cancellationToken) =>
{
    try
    {
        var suggestion = await advisor.GetSuggestionAsync(returnOrderId, cancellationToken);
        return Results.Ok(suggestion);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
});

app.MapPost("/internal/runtime/dispositions/{returnOrderId:guid}/execute", async (
    Guid returnOrderId,
    ExecuteDispositionRequest request,
    ReturnDispositionExecutor executor,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await executor.ExecuteAsync(returnOrderId, request, cancellationToken);
        return Results.Ok(result);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
});

app.MapPost("/internal/runtime/sop/{sessionId:guid}/steps", async (
    Guid sessionId,
    AdvanceSopStepRequest request,
    SopAssistService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.AdvanceAsync(sessionId, request, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/internal/runtime/sop/{sessionId:guid}/events", async (
    Guid sessionId,
    HttpResponse response,
    SseEventWriter writer,
    CancellationToken cancellationToken) =>
{
    response.ContentType = "text/event-stream";
    await writer.WriteAsync(response, AgUiEventMapper.MapHeartbeat(sessionId), cancellationToken);
});

app.MapWmsDefaultEndpoints();
app.Run();

public sealed record RuntimeFailureCountResponse(int Count);

public partial class Program;
