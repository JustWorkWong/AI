using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Runtime.Tests;

public sealed class ProgramProblemDetailsTests
{
    [Fact]
    public async Task Create_problem_result_should_emit_not_found_problem_details_with_trace_id()
    {
        var (response, body) = await ExecuteAsync(
            global::Program.CreateProblemResult(
                CreateContext("trace-not-found"),
                StatusCodes.Status404NotFound,
                "Return order not found",
                "Return order '00000000-0000-0000-0000-000000000000' does not exist."),
            CreateContext("trace-not-found"));

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentType);
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("error", out _));
        Assert.Equal("trace-not-found", body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Create_problem_result_should_emit_bad_request_problem_details_with_trace_id()
    {
        var (response, body) = await ExecuteAsync(
            global::Program.CreateProblemResult(
                CreateContext("trace-bad-request"),
                StatusCodes.Status400BadRequest,
                "Unsupported approval action",
                "Action 'Maybe' is not supported."),
            CreateContext("trace-bad-request"));

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentType);
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("error", out _));
        Assert.Equal("trace-bad-request", body.RootElement.GetProperty("traceId").GetString());
    }

    private static DefaultHttpContext CreateContext(string traceId)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = traceId;
        return context;
    }

    private static async Task<(HttpResponse Response, JsonDocument Body)> ExecuteAsync(
        IResult result,
        DefaultHttpContext context)
    {
        context.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        await using var stream = new MemoryStream();
        context.Response.Body = stream;

        await result.ExecuteAsync(context);

        stream.Position = 0;
        var body = await JsonDocument.ParseAsync(stream);
        return (context.Response, body);
    }
}
