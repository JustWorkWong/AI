using Wms.DomainService.Integration;

namespace Wms.DomainService.UnitTests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void New_outbox_message_should_start_as_pending()
    {
        var message = OutboxMessage.Create("user-synced", "{}");

        Assert.Equal("Pending", message.Status);
    }
}
