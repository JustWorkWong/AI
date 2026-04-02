namespace Wms.DomainService.Auth;

public sealed class Permission
{
    public Permission(Guid id, string code)
    {
        Id = id;
        Code = code;
    }

    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;
}
