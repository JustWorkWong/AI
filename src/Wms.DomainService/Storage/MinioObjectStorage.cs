using Minio;
using Minio.DataModel.Args;

namespace Wms.DomainService.Storage;

public sealed class MinioObjectStorage(IMinioClient client) : IObjectStorage
{
    public async Task<string> PutAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            var buffered = new MemoryStream();
            await content.CopyToAsync(buffered, cancellationToken);
            buffered.Position = 0;
            content = buffered;
        }

        await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket("return-attachments")
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
            cancellationToken);

        return key;
    }
}
