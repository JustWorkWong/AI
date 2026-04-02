using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Approvals;
using Wms.DomainService.Auth;
using Wms.DomainService.Endpoints;
using Wms.DomainService.Persistence;
using Wms.DomainService.Seed;
using Wms.DomainService.Storage;
using Wms.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddWmsServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<WmsDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("wmsdb")
        ?? builder.Configuration["ConnectionStrings:wmsdb"]
        ?? "Host=localhost;Database=wmsdb;Username=postgres;Password=postgres";

    options.UseNpgsql(connectionString);
});

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddSingleton<IObjectStorage, NoOpObjectStorage>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await DevelopmentBootstrapper.InitializeAsync(app.Services, app.Environment);

app.MapGet("/healthz", () => Results.Ok("ok"));

app.MapPost("/internal/auth/sync", async (
    SyncUserRequest request,
    WmsDbContext db,
    CancellationToken cancellationToken) =>
{
    var user = await db.Users.SingleOrDefaultAsync(
        x => x.ExternalSubject == request.ExternalSubject,
        cancellationToken);

    if (user is null)
    {
        user = new User(Guid.NewGuid(), request.ExternalSubject, request.UserName);
        db.Users.Add(user);
    }

    user.UpdateProfile(request.UserName, request.DisplayName);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Accepted();
});

app.MapDispositionEndpoints();
app.MapApprovalEndpoints();
app.MapAttachmentEndpoints();
app.MapReturnReadEndpoints();
app.MapSopPublishEndpoints();
app.MapSopReadEndpoints();
app.MapWmsDefaultEndpoints();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/internal/test/fault", () =>
    {
        throw new InvalidOperationException("Test fault");
    });
}

app.Run();

public partial class Program
{
    public static IResult CreateProblemResult(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            });
    }
}
