namespace Wms.DomainService.Storage;

public sealed class NoOpObjectStorage : IObjectStorage
{
    public Task<string> PutAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken) =>
        Task.FromResult(key);
}
