using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Wms.DomainService.Persistence;
using Wms.DomainService.Storage;

namespace Wms.DomainService.IntegrationTests;

public sealed class AttachmentEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public AttachmentEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Upload_should_store_metadata_and_enqueue_outbox_message()
    {
        var storage = new FakeObjectStorage();
        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString, services =>
        {
            services.AddSingleton<IObjectStorage>(storage);
        });

        var client = app.CreateClient();
        var returnOrderId = Guid.NewGuid();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("abc")));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");

        var response = await client.PostAsync($"/internal/returns/{returnOrderId}/attachments", content);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Single(storage.StoredKeys);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        var attachment = db.ReturnAttachments.Single(x => x.ReturnOrderId == returnOrderId);
        var message = db.OutboxMessages.Single(x => x.EventType == "return-attachment-uploaded");

        Assert.Equal("photo.jpg", attachment.FileName);
        Assert.Equal("image/jpeg", attachment.ContentType);
        Assert.Equal("Pending", message.Status);
    }

    private sealed class FakeObjectStorage : IObjectStorage
    {
        public List<string> StoredKeys { get; } = [];

        public Task<string> PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken)
        {
            StoredKeys.Add(key);
            return Task.FromResult(key);
        }
    }
}
