namespace Wms.DomainService.Auth;

public sealed class User
{
    public User(Guid id, string externalSubject, string userName)
    {
        Id = id;
        ExternalSubject = externalSubject;
        UserName = userName;
        DisplayName = userName;
    }

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string ExternalSubject { get; private set; } = string.Empty;

    public string UserName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public void UpdateProfile(string userName, string displayName)
    {
        UserName = userName;
        DisplayName = displayName;
    }
}
