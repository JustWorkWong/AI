namespace Wms.DomainService.Auth;

public sealed class Role
{
    public Role(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
