using Microsoft.EntityFrameworkCore;
using Agent.Runtime.Models;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
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
builder.Services.AddScoped<IModelGateway, ModelGateway>();
builder.Services.AddScoped<IToolInvocationStore, EfToolInvocationStore>();
builder.Services.AddSingleton<ConversationCompactor>();
builder.Services.AddScoped<ToolLoggingMiddleware>();
builder.Services.AddSingleton<SseEventWriter>();

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

app.MapGet("/internal/runtime/dispositions/{returnOrderId:guid}", (Guid returnOrderId) =>
{
    return Results.Ok(new DispositionSuggestionDto(
        returnOrderId,
        "Scrap",
        "High",
        [],
        "Pending"));
});

app.MapPost("/internal/runtime/sop/{sessionId:guid}/steps", (Guid sessionId, AdvanceSopStepRequest request) =>
{
    return Results.Ok(new SopExecutionViewDto(
        sessionId,
        "RETURNS",
        request.StepCode,
        [],
        true));
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
