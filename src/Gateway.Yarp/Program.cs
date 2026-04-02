using Wms.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddWmsServiceDefaults();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();
app.MapGet("/healthz", () => Results.Ok("ok"));
app.MapWmsDefaultEndpoints();

app.Run();
