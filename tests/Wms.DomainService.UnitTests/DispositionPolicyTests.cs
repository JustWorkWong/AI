using Wms.DomainService.Returns;

namespace Wms.DomainService.UnitTests;

public sealed class DispositionPolicyTests
{
    [Theory]
    [InlineData("Broken", "Scrap")]
    [InlineData("Unopened", "Resell")]
    public void Policy_should_map_quality_state_to_allowed_outcome(string condition, string expected)
    {
        var outcome = DispositionPolicy.Resolve(condition);

        Assert.Equal(expected, outcome);
    }
}
