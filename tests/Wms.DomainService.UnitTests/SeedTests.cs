using Wms.DomainService.Seed;

namespace Wms.DomainService.UnitTests;

public sealed class SeedTests
{
    [Fact]
    public void Default_roles_should_include_ai_admin()
    {
        var roles = Seeder.DefaultRoles();

        Assert.Contains("AiAdmin", roles);
    }
}
