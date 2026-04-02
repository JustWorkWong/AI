namespace Wms.DomainService.Storage;

public interface IObjectStorage
{
    Task<string> PutAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);
}
