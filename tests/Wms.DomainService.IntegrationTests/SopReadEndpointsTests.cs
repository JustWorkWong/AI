using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Sop;
using Wms.DomainService.Persistence;
using Wms.DomainService.Sop;

namespace Wms.DomainService.IntegrationTests;

public sealed class SopReadEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SopReadEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Search_sop_candidates_should_return_published_documents_for_operation_and_step()
    {
        var documentId = Guid.NewGuid();
        var operationCode = $"RETURNS-{Guid.NewGuid():N}";
        var stepCode = $"INSPECT-{Guid.NewGuid():N}";

        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            db.SopDocuments.Add(new SopDocument(documentId, "SOP-RET-001", operationCode, "v1", "退货质检"));
            db.SopChunks.Add(new SopChunk(Guid.NewGuid(), documentId, stepCode, 1, "核对外观是否破损"));
            db.SopDocuments.Add(new SopDocument(Guid.NewGuid(), "SOP-RET-002", "PUTAWAY", "v1", "上架作业"));
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();

        var response = await client.GetAsync($"/internal/sop/candidates?operationCode={operationCode}&stepCode={stepCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<SopCandidateDto>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal(documentId, payload[0].DocumentId);
        Assert.Equal("SOP-RET-001", payload[0].DocumentCode);
    }

    [Fact]
    public async Task Retrieve_sop_chunks_should_return_chunks_for_requested_candidates()
    {
        var documentId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var operationCode = $"RETURNS-{Guid.NewGuid():N}";
        var stepCode = $"INSPECT-{Guid.NewGuid():N}";

        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            db.SopDocuments.Add(new SopDocument(documentId, "SOP-RET-003", operationCode, "v2", "退货复检"));
            db.SopChunks.Add(new SopChunk(chunkId, documentId, stepCode, 1, "确认屏幕、边框与序列号"));
            db.SopChunks.Add(new SopChunk(Guid.NewGuid(), documentId, "PACK", 2, "确认包装耗材"));
            await db.SaveChangesAsync();
        }

        var client = app.CreateClient();
        var query = new RetrieveSopChunksQuery(operationCode, stepCode, [documentId]);

        var response = await client.PostAsJsonAsync("/internal/sop/chunks/search", query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<SopChunkDto>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal(chunkId, payload[0].ChunkId);
        Assert.Equal(stepCode, payload[0].StepCode);
    }
}
