using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Sop;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.IntegrationTests;

public sealed class SopPublishEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SopPublishEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Publish_should_persist_chunks_that_can_be_retrieved_by_step()
    {
        var operationCode = $"RETURNS-{Guid.NewGuid():N}";
        var stepCode = "STEP-01";

        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        var client = app.CreateClient();

        var publishResponse = await client.PostAsJsonAsync("/internal/sop/publish", new PublishSopCommand(
            $"SOP-RET-{Guid.NewGuid():N}"[..16],
            operationCode,
            "v1",
            "退货质检标准作业",
            "STEP-01|检查包装完整性。STEP-02|确认序列号。"));

        Assert.Equal(HttpStatusCode.Accepted, publishResponse.StatusCode);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            Assert.Single(db.SopDocuments.Where(x => x.OperationCode == operationCode));
            Assert.Equal(2, db.SopChunks.Count(x => x.StepCode.StartsWith("STEP-")));
        }

        var candidatesResponse = await client.GetAsync($"/internal/sop/candidates?operationCode={operationCode}&stepCode={stepCode}");
        Assert.Equal(HttpStatusCode.OK, candidatesResponse.StatusCode);
        var candidates = await candidatesResponse.Content.ReadFromJsonAsync<IReadOnlyList<SopCandidateDto>>();
        Assert.NotNull(candidates);
        Assert.Single(candidates!);

        var chunksResponse = await client.PostAsJsonAsync(
            "/internal/sop/chunks/search",
            new RetrieveSopChunksQuery(operationCode, stepCode, [candidates[0].DocumentId]));

        Assert.Equal(HttpStatusCode.OK, chunksResponse.StatusCode);
        var chunks = await chunksResponse.Content.ReadFromJsonAsync<IReadOnlyList<SopChunkDto>>();
        Assert.NotNull(chunks);
        Assert.Single(chunks!);
        Assert.Equal(stepCode, chunks[0].StepCode);
        Assert.Equal("检查包装完整性。", chunks[0].Content);
    }
}
