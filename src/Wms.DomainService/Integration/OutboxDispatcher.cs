using Wms.DomainService.Persistence;

namespace Wms.DomainService.Integration;

public sealed class OutboxDispatcher(WmsDbContext dbContext)
{
    public Task<int> PendingCountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(dbContext.OutboxMessages.Count(x => x.Status == "Pending"));
}
